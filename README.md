# Rick Gibbed's JustCause 3 Tools // fork

This is a fork of the Just Cause 3 tools by Rick Gibbed as of their revision 13.
You can find the original version of the tools on the Rick's svn.
Those sources also contains the RBM Deserializer from the Just Cause 2 tools by
Rick Gibbed, also modified. You can find the original version on the Rick's
Just Cause 2 SVN repository.

The original work by Rick Gibbed is Licensed under the ZLib license (see the
license file) <br/>
The original work on the RBM Deserializer by Rick Gibbed is Licensed under the
ZLib license (see the license file in the corresponding folder) <br/>
The modifications of the original sources are licensed under the same license
but are the work of Timothée Feuillet.


# Changes

Changes includes:
  - A (working) and generic ADF serializer and deserializer
  - Some changes on the RTPC Serializer to make is compatible with the Avalanche
    RTPC file format
  - A texture serializer and deserializer that supports both `.ddsc` files and
    their `.hmddsc` counterparts
  - A .arc and .tab Packer
  - Some fixes on the AAF packer and unpacker
  - Adding compression support on the AAF packer
  - A scrapper that parses the games files
  - A WIP (in its ultra-very early stage) of a RBM serializer and deserializer
  - Updated .filelist files
  - A .namelist file with 16% of RTPC identifier covered
  - A `SkyFortressPacker` executable for an easy management of the mods for the
    SKy Fortress DLC
  - Add some utilities (`Batch`, `RecurseBatchAndPack`, `` HashName`, ...)
    to help the modders
  - A `ConvertStringLookup` executable for an easier edition of `stringlookup`
    files
  - Some other changes


# Binary Releases

Official binary release are to be found
[http://justcause3mods.com/mods/modified-gibbeds-tools/](here) but there's
nothing to stop you from building these tools from source.


# Future

Future changes will/may include:
  - A serializer and deserializer for rbm files (rbm <-> obj)
  - A comprehensive editing of `object-id` values of RTPC files
  - Support for editing ADF files that holds map information
  - A *possible* support of a mod editor
    - it would be able to load and export changes on AAF files
      (.ee files mostly)
    - render the map and create wingsuit/races events
    - modify the map

And almost everything submitted.

