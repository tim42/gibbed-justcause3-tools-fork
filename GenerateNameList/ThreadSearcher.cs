﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using Gibbed.JustCause3.PropertyFormats;
using Gibbed.ProjectData;

namespace GenerateNameList
{
    internal class ThreadSearcher
    {
        private static Mutex _PrintMutex = new Mutex(false);

        private HashSet<string> _StringList;
        private HashSet<string> _StringLookupList;
        private Dictionary<string, HashSet<uint>> _HashList;

        private List<string> _FileList;
        private HashList<uint> _ProjectFileList;

        private Thread _SelfThread;
        private uint _ThreadIndex;
        private uint _ThreadCount;
        private int _LineOffset;

        private Dictionary<uint, HashSet<uint>> _HashFileHash;
        private HashSet<string> _FileSet;
        private string _FlushFile; // this is to avoid consuming too much memory
        private uint _EntryCount;

        private List<string> _CurrentFile; // the path (including any AAF archive) of the file
        private uint _CurrentFileHash;
        private string _CurrentFileString;

        private int _ArchiveIndex;
        private int _ArchiveEntryIndex;
        private int _ArchiveEntryCount;
        private int _OldFileCount;

        // stats
        public uint ADFReadCount;
        public uint ADFSkipCount;
        public uint RTPCReadCount;
        public uint RTPCSkipCount;
        public uint AAFExtractCount;
        public int FileCount;

        // getters
        public Dictionary<uint, HashSet<uint>> HashFileHash { get { return this._HashFileHash; } }
        public HashSet<string> FileSet { get { return this._FileSet; } }
        public HashSet<string> StringList { get { return this._StringList; } }
        public HashSet<string> StringLookupList { get { return this._StringLookupList; } }
        public Dictionary<string, HashSet<uint>> HashList { get { return this._HashList; } }

        public string FlushFile { get { return this._FlushFile; } }

        public ThreadSearcher(uint index, uint count, HashList<uint> fileList)
        {
            this._StringList = new HashSet<string>();
            this._StringLookupList = new HashSet<string>();
            this._HashList = new Dictionary<string, HashSet<uint>>();
            this._FileList = new List<string>();
            this._CurrentFile = new List<string>();
            this._HashFileHash = new Dictionary<uint, HashSet<uint>>();
            this._FileSet = new HashSet<string>();
            this._ProjectFileList = fileList;
            this._ThreadIndex = index;
            this._ThreadCount = count;

            this._FlushFile = Path.GetTempFileName();

            // reset stats
            this.ADFReadCount = 0;
            this.ADFSkipCount = 0;
            this.RTPCReadCount = 0;
            this.RTPCSkipCount = 0;
            this.AAFExtractCount = 0;
            this.FileCount = 0;
        }

        public void Search(List<string> fileList)
        {
            int index = (fileList.Count / (int)this._ThreadCount) * (int)this._ThreadIndex;
            int count = fileList.Count / (int)this._ThreadCount;
            if (this._ThreadIndex + 1 == this._ThreadCount)
                count = fileList.Count - index; // get the remaining elements

            this._FileList = fileList.GetRange(index, count);
            this._HashList.Clear();
            this._StringList.Clear();
            this._LineOffset = Console.CursorTop;
            this._OldFileCount = -1000;
            this._CurrentFileHash = 0;
            this._CurrentFileString = null;

            Console.WriteLine("[th{0}] -- starting...", this._ThreadIndex + 1);

            this._SelfThread = new Thread(this._DoSearch);
            this._SelfThread.Start();
        }

        public void Wait()
        {
            this._SelfThread.Join();
        }

        // the thread entry point
        private void _DoSearch()
        {
            this._ArchiveIndex = 0;

            this._CurrentFile.Add(null); // the arc file
            this._CurrentFile.Add(null); // the file inside the arc file

            // walk the TAB files
            foreach (var tabFile in this._FileList)
            {
                var tab = new ArchiveTableFile();
                using (var input = File.OpenRead(tabFile))
                {
                    tab.Deserialize(input);
                }

                var arcFile = Path.ChangeExtension(tabFile, ".arc");
                this._CurrentFile[0] = Path.GetFileName(Path.GetDirectoryName(arcFile)) + '/' + Path.GetFileName(arcFile);
                using (var input = File.OpenRead(arcFile))
                {
                    this._ArchiveEntryIndex = 0;
                    this._ArchiveEntryCount = tab.Entries.Count;

                    foreach (var entry in tab.Entries)
                    {
                        ++this._ArchiveEntryIndex;
                        ++this.FileCount;
                        this.PrintProgress();

                        input.Position = entry.Offset;
                        string subtype = this.GetType(input);
                        if (string.IsNullOrEmpty(subtype))
                            continue;

                        // set the current file
                        if (this._ProjectFileList.Contains(entry.NameHash))
                            this._CurrentFile[1] = this._ProjectFileList[entry.NameHash];
                        else
                            this._CurrentFile[1] = entry.NameHash.ToString("X8") + '.' + subtype;
                        this.UpdateCurrentFile();
                        // process that file
                        using (var entryStream = entry.ReadToMemoryStream(input))
                        {
                            this.HandleStream(entryStream, subtype);
                        }
                    }
                }
                ++this._ArchiveIndex;
            }
            this.flush();
            this.PrintEnd();
        }

        private void HandleStream(Stream file, string type)
        {
            // flush if the limit is reached
            if (this._EntryCount > 300000)
                this.flush();

            //file.Position = 0;
            if (type == "ADF")
            {
                // Got some AAF file
                bool inc = false;
                try
                {
                    var adf = new AdfFile();
                    try
                    {
                        adf.Deserialize(file);
                        ++ADFReadCount;
                    }
                    catch
                    {
                        inc = true;
                        ++ADFSkipCount;
                    }
                    ADFExtractStrings(adf);
                }
                catch
                {
                    if (!inc)
                        ++ADFSkipCount;
                }
            }
            else if (type == "RTPC")
            {
                // Got some RTPC file
                try
                {
                    var propertyContainerFile = new PropertyContainerFile();
                    propertyContainerFile.Deserialize(file);
                    RTPCExtractStrings(propertyContainerFile);
                    ++RTPCReadCount;
                }
                catch
                {
                    ++RTPCSkipCount;
                }
            }
            else if (type == "AAF")
            {
                // Got some AAF Archive
                using (var cool = CreateCoolArchiveStream(file))
                {
                    var input = cool ?? file;

                    var smallArchive = new SmallArchiveFile();
                    smallArchive.Deserialize(input);

                    this._CurrentFile.Add(null);

                    foreach (var entry in smallArchive.Entries)
                    {
                        this.AddString(entry.Name);
                        if (entry.Offset == 0)
                            continue;

                        ++this.FileCount;
                        this.PrintProgress();

                        input.Position = entry.Offset;
                        string subtype = this.GetType(input);
                        if (string.IsNullOrEmpty(subtype))
                            continue;
                        // insert the name
                        this._CurrentFile[this._CurrentFile.Count - 1] = entry.Name;
                        this.UpdateCurrentFile();
                        // process the file
                        using (var entryStream = entry.ReadToMemoryStream(input))
                        {
                            // I know that this copies a copy of a memory (that's a stupid thing)
                            // but without this the memories goes crazy and if I force GC Collect
                            // that's even worse. So a little copy doesn't look bad in front of
                            // the horrors produced when you don't have it.
                            // to see what it's like, replace entryStream with input.
                            this.HandleStream(entryStream, subtype);
                        }
                    }

                    this._CurrentFile.RemoveAt(this._CurrentFile.Count - 1);

                    ++this.AAFExtractCount;
                }
            }
        }

        private void AddString(string s)
        {
            if (!this._StringList.Contains(s))
                this._StringList.Add(s);

            LinkHashToFile(s.HashJenkins());
        }

        private void AddHash(uint h, string name)
        {
            if (h != 0 && !this._HashList.ContainsKey(name))
                this._HashList[name] = new HashSet<uint>() { h };
            else if (h != 0 && !this._HashList[name].Contains(h))
                this._HashList[name].Add(h);

            LinkHashToFile(h);
        }

        private void LinkHashToFile(uint h)
        {
            if (h != 0)
            {
                // create the entry for the file and the hash
                if (this._HashFileHash.ContainsKey(h))
                {
                    if (this._HashFileHash[h].Contains(this._CurrentFileHash) == false)
                    {
                        this._HashFileHash[h].Add(this._CurrentFileHash);
                        ++this._EntryCount;
                    }
                }
                else
                {
                    this._HashFileHash.Add(h, new HashSet<uint>() { this._CurrentFileHash });
                    ++this._EntryCount;
                }
                // insert the file
                if (!this._FileSet.Contains(this._CurrentFileString))
                    this._FileSet.Add(this._CurrentFileString);
            }
        }

        private void ADFExtractStrings(AdfFile adf)
        {
            // load strings from the hash string table
            foreach (var stringItem in adf.StringHashInfos)
                AddString(stringItem.Value);
            // load strings from type definitions (why ? because an ADF file can contain types that aren't used !)
            foreach (var typeDef in adf.TypeDefinitions)
            {
                AddString(typeDef.Name);
                if (typeDef.Type != AdfFile.TypeDefinitionType.Structure)
                    continue;
                foreach (var member in typeDef.Members)
                    AddString(member.Name);
            }
            // load strings from the instance members (unfold the whole tree)
            foreach (var instance in adf.InstanceInfos)
            {
                AddString(instance.Name);
                foreach (var rootMember in instance.Members)
                {
                    var imiQueue = new Queue<AdfFile.InstanceMemberInfo>();
                    imiQueue.Enqueue(rootMember);

                    while (imiQueue.Count > 0)
                    {
                        var member = imiQueue.Dequeue();

                        if (!string.IsNullOrEmpty(member.Name))
                        {
                            AddString(member.Name);
                            AddHash(member.Name.HashJenkins(), "adf-property-names");
                        }

                        if (!string.IsNullOrEmpty(member.StringData))
                        {
                            AddString(member.StringData);
                            AddHash(member.StringData.HashJenkins(), "adf-strings");
                        }

                        if (member.Type == AdfFile.TypeDefinitionType.StringHash
                            || (member.Type == AdfFile.TypeDefinitionType.Primitive && member.TypeHash == AdfTypeHashes.String))
                            AddHash(member.StringData.HashJenkins(), "adf-string-hashes");
                        else if (member.Type == AdfFile.TypeDefinitionType.Primitive)
                        {
                            if (member.TypeHash == AdfTypeHashes.Primitive.Int32 || member.TypeHash == AdfTypeHashes.Primitive.UInt32)
                            {
                                member.Data.Position = 0;
                                uint value = member.Data.ReadValueU32();
                                member.Data.Position = 0;
                                AddHash(value, "adf-integer-values");
                            }
                        }

                        // this is a special case for stringlookup files, where there's a sometime a
                        // HUGE int8 array that contains and UTF-8 encoded string.
                        // so for the sake of finding the more string, we will need to parse
                        // int8 arrays that ends with a null byte
                        // We may be poluted with strings from other languages...
                        bool isString = false;

                        if (member.Type == AdfFile.TypeDefinitionType.Array && !member.isReferenceToId && member.Members.Count > 0
                            && (member.TypeDef.ElementTypeHash == AdfTypeHashes.Primitive.Int8 || member.TypeDef.ElementTypeHash == AdfTypeHashes.Primitive.UInt8))
                        {
                            // check if that's a string: it should end with \0
                            sbyte last = (sbyte)member.Members.Last().Data.ReadByte();
                            member.Members.Last().Data.Position = 0;
                            if (last == 0)
                            {
                                isString = true;
                                List<byte> accum = new List<byte>();
                                foreach (var character in member.Members)
                                {
                                    byte current = (byte)character.Data.ReadByte();
                                    if (current != 0)
                                        accum.Add(current);
                                    else // write the string
                                    {
                                        var str = Encoding.UTF8.GetString(accum.ToArray());
                                        if (!this._StringLookupList.Contains(str))
                                            this._StringLookupList.Add(str);
                                        AddHash(str.HashJenkins(), "adf-stringlookup-text");
                                        accum.Clear();
                                    }
                                }
                            }
                        }

                        // if that's not a hidden string array:
                        if (!isString)
                        {
                            foreach (var subMember in member.Members)
                                imiQueue.Enqueue(subMember);
                        }
                    }
                }
            }
        }

        private void RTPCExtractStrings(PropertyContainerFile rtpc)
        {
            var nodeQueue = new Queue<Node>();
            nodeQueue.Enqueue(rtpc.Root);

            while (nodeQueue.Count > 0)
            {
                var node = nodeQueue.Dequeue();

                AddHash(node.NameHash, "identifiers");

                foreach (var subs in node.Children)
                    nodeQueue.Enqueue(subs);
                foreach (var property in node.Properties)
                {
                    var hashes = property.Value.GetHashList();

                    switch (property.Value.Tag)
                    {
                        case "vec_int":
                            if (hashes != null && hashes.Length > 0)
                            {
                                for (int i = 0; i < hashes.Length; ++i)
                                    AddHash(hashes[i], "integer-values");
                            }
                            break;
                        case "int":
                            if (hashes != null && hashes.Length > 0)
                                AddHash(hashes[0], "integer-values");
                            break;
                        case "objectid":
                            if (hashes != null && hashes.Length > 0)
                            {
                                AddHash(hashes[0], "object-ids");
                                AddHash(hashes[1], "object-id-values");
                            }
                            break;
                        case "vec_events":
                            if (hashes != null && hashes.Length > 0)
                            {
                                for (int i = 0; i < hashes.Length; i += 2)
                                {
                                    AddHash(hashes[i + 0], "events");
                                    AddHash(hashes[i + 1], "event-values");
                                }
                            }
                            break;
                        case "string":
                            var str = property.Value.Compose(new Gibbed.ProjectData.HashList<uint>());
                            if (!string.IsNullOrEmpty(str))
                            {
                                AddString(str);
                                AddHash(str.HashJenkins(), "rtpc-strings");
                            }
                            break;
                    }

                    AddHash(property.Key, "identifiers");
                }
            }
        }

        private void flush()
        {
            this.PrintFlush();

            this._EntryCount = 0;
            using (var output = File.OpenWrite(this._FlushFile))
            {
                output.Position = output.Length;

                output.WriteValueU32((uint)this._HashFileHash.Count);
                foreach (var kv in this._HashFileHash)
                {
                    output.WriteValueU32(kv.Key);
                    output.WriteValueU32((uint)kv.Value.Count);

                    foreach (var h in kv.Value)
                        output.WriteValueU32(h);
                }
            }
            this._HashFileHash.Clear();
            GC.Collect();
        }

        private static Stream CreateCoolArchiveStream(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);
            var isCoolArchive = CoolArchiveFile.CheckHeader(input);
            input.Seek(0, SeekOrigin.Begin);

            if (isCoolArchive == false)
            {
                return null;
            }

            var archive = new CoolArchiveFile();
            archive.Deserialize(input);

            return new CoolStream(archive, input);
        }

        private void UpdateCurrentFile()
        {
            StringBuilder sb = new StringBuilder();

            bool first = true;
            foreach (string s in this._CurrentFile)
            {
                if (s == null)
                    break;
                if (!first)
                    sb.Append(':');
                sb.Append(s);
                first = false;
            }
            this._CurrentFileString = sb.ToString();
            this._CurrentFileHash = this._CurrentFileString.HashJenkins();
        }

        private string GetType(Stream s)
        {
            var p = s.Position;
            uint rd = s.ReadValueU32();
            s.Position = p;

            switch (rd)
            {
                case AdfFile.Signature:
                    return "ADF";
                case PropertyContainerFile.Signature:
                    return "RTPC";
                case CoolArchiveFile.Signature:
                    return "AAF";
            }
            switch (rd.Swap())
            {
                case AdfFile.Signature:
                    return "ADF";
                case PropertyContainerFile.Signature:
                    return "RTPC";
                case CoolArchiveFile.Signature:
                    return "AAF";
            }
            return null;
        }

        private void PrintProgress()
        {
            float singleArcProgress = (float)this._ArchiveIndex / (float)this._FileList.Count;
            float progressInArc = (float)this._ArchiveEntryIndex / (float)this._ArchiveEntryCount;
            float globalProgress = ((float)this._ArchiveIndex + progressInArc) * 100 / (float)this._FileList.Count;
            if (this.FileCount >= this._OldFileCount + 100)
            {
                _PrintMutex.WaitOne();
                Console.CursorTop = this._LineOffset;
                Console.CursorLeft = 0;
                Console.WriteLine("[th{0}] [{3,4:0.#}%] -- {1}:\t[{4,4:0.#}%] [{2} files]\t<{5} AAF><{6} ADF><{7} RTPC>",
                    this._ThreadIndex + 1, Path.GetFileName(this._FileList[this._ArchiveIndex]),
                    this.FileCount,
                    globalProgress, progressInArc * 100,
                    this.AAFExtractCount, this.ADFReadCount, this.RTPCReadCount);
                this._OldFileCount = this.FileCount;
                _PrintMutex.ReleaseMutex();
            }
        }
        private void PrintEnd()
        {
            _PrintMutex.WaitOne();
            Console.CursorTop = this._LineOffset;
            Console.CursorLeft = 0;
            Console.WriteLine("[---] [ 100%] -- FINISHED:\t[ ---%] [{0} files]\t<{1} AAF><{2} ADF><{3} RTPC>",
                this.FileCount,
                this.AAFExtractCount, this.ADFReadCount, this.RTPCReadCount);
            this._OldFileCount = this.FileCount;
            _PrintMutex.ReleaseMutex();
        }
        private void PrintFlush()
        {
            float singleArcProgress = (float)this._ArchiveIndex / (float)this._FileList.Count;
            float progressInArc = (float)this._ArchiveEntryIndex / (float)this._ArchiveEntryCount;
            float globalProgress = ((float)this._ArchiveIndex + progressInArc) * 100 / (float)this._FileList.Count;
            _PrintMutex.WaitOne();
            Console.CursorTop = this._LineOffset;
            Console.CursorLeft = 0;
            Console.WriteLine("[th{0}] [{1,4:0.#}%] -- FLUSHING:\t[ ---%] [{2} files]\t<{3} AAF><{4} ADF><{5} RTPC>",
                this._ThreadIndex + 1,
                globalProgress,
                this.FileCount,
                this.AAFExtractCount, this.ADFReadCount, this.RTPCReadCount);
            this._OldFileCount = this.FileCount;
            _PrintMutex.ReleaseMutex();
        }
    }
}
