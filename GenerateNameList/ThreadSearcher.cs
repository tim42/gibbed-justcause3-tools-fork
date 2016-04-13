using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using Gibbed.JustCause3.PropertyFormats;

namespace GenerateNameList
{
    internal class ThreadSearcher
    {
        private static Mutex _PrintMutex = new Mutex(false);

        private HashSet<string> _StringList;
        private HashSet<string> _StringLookupList;
        private HashSet<uint> _HashList;
        private List<string> _FileList;
        private Thread _SelfThread;
        private uint _ThreadIndex;
        private uint _ThreadCount;
        private int _LineOffset;

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

        public ThreadSearcher(uint index, uint count)
        {
            this._StringList = new HashSet<string>();
            this._StringLookupList = new HashSet<string>();
            this._HashList = new HashSet<uint>();
            this._FileList = new List<string>();
            this._ThreadIndex = index;
            this._ThreadCount = count;

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

            Console.WriteLine("[th{0}] -- starting...", this._ThreadIndex + 1);

            this._SelfThread = new Thread(this._DoSearch);
            this._SelfThread.Start();
        }

        public void Wait()
        {
            this._SelfThread.Join();
        }

        public void MergeResults(Stream stringOutputStream, SortedSet<string> foundStrings, Stream hashOutputStream, SortedSet<uint> foundHashes,
                                 Stream stringLookupOutputStream)
        {
            // string
            foreach (var str in this._StringList)
            {
                if (!string.IsNullOrEmpty(str) && !foundStrings.Contains(str))
                {
                    foundStrings.Add(str);
                    stringOutputStream.WriteString(str);
                    stringOutputStream.WriteString(Environment.NewLine);
                }
            }

            // string (from stringlookup files)
            foreach (var str in this._StringLookupList)
            {
                if (!string.IsNullOrEmpty(str) && !foundStrings.Contains(str))
                {
                    foundStrings.Add(str);
                    stringLookupOutputStream.WriteString(str);
                    stringLookupOutputStream.WriteString(Environment.NewLine);
                }
            }

            // hash
            foreach (var hash in this._HashList)
            {
                if (hash != 0 && !foundHashes.Contains(hash))
                {
                    foundHashes.Add(hash);
                    hashOutputStream.WriteValueU32(hash);
                }
            }
        }

        // the thread entry point
        private void _DoSearch()
        {
            this._ArchiveIndex = 0;

            // walk the TAB files
            foreach (var tabFile in this._FileList)
            {
                var tab = new ArchiveTableFile();
                using (var input = File.OpenRead(tabFile))
                {
                    tab.Deserialize(input);
                }

                var arcFile = Path.ChangeExtension(tabFile, ".arc");
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
                        using (var entryStream = entry.ReadToMemoryStream(input))
                        {
                            this.HandleStream(entryStream, subtype);
                        }
                    }
                }
                ++this._ArchiveIndex;
            }
            this.PrintEnd();
        }

        private void HandleStream(Stream file, string type)
        {
            file.Position = 0;
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
                        using (var entryStream = entry.ReadToMemoryStream(input))
                        {
                            this.HandleStream(entryStream, subtype);
                        }
                    }

                    ++this.AAFExtractCount;
                }
            }
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

        private void AddString(string s)
        {
            if (!this._StringList.Contains(s))
                this._StringList.Add(s);
        }

        private void AddHash(uint h)
        {
            if (!this._HashList.Contains(h))
                this._HashList.Add(h);
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

                        AddString(member.Name);
                        AddString(member.StringData);

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

                AddHash(node.NameHash);

                foreach (var subs in node.Children)
                    nodeQueue.Enqueue(subs);
                foreach (var property in node.Properties)
                {
                    // uncomment to also put object ID / vec events keys and values in the hash list
                    //var hashes = property.Value.GetHashList();
                    //if (hashes != null)
                    //{
                    //    foreach (uint hash in hashes)
                    //        AddHash(hash);
                    //}

                    if (property.Value.Tag == "string")
                        AddString(property.Value.Compose(new Gibbed.ProjectData.HashList<uint>()));

                    AddHash(property.Key);
                }
            }
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
    }
}
