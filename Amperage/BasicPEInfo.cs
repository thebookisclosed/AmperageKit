using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Amperage
{
    internal class BasicPEInfo
    {
        internal long ChecksumPosition { get; set; }
        internal List<PESectionInfo> Sections { get; set; }

        private readonly Dictionary<string, PESectionInfo> _sectionMap;
        private BasicPEInfo(long checksumPosition, List<PESectionInfo> sectionList)
        {
            ChecksumPosition = checksumPosition;
            Sections = sectionList;
            _sectionMap = new Dictionary<string, PESectionInfo>();
            for (int i = 0; i < Sections.Count; i++)
                _sectionMap[Sections[i].Name] = Sections[i];
        }

        internal static BasicPEInfo Parse(BinaryReader binReader)
        {
            List<PESectionInfo> sectionInfo = null;
            long checksumPosition = 0;
            var baseFs = binReader.BaseStream;
            var restorePos = baseFs.Position;
            baseFs.Position = 0;
            if (baseFs.Length > 2 && binReader.ReadUInt16() == 0x5A4D)
            {
                baseFs.Position = 0x3C;
                baseFs.Position = binReader.ReadInt32() + 6;
                var use64BAddr = false;
                var peSectionCount = binReader.ReadUInt16();
                baseFs.Position += 0x10;
                if (binReader.ReadUInt16() == 0x020B)
                    use64BAddr = true;
                checksumPosition = baseFs.Position + 0x3E;
                baseFs.Position += use64BAddr ? 0xEE : 0xDE;
                sectionInfo = new List<PESectionInfo>();
                for (int i = 0; i < peSectionCount; i++)
                {
                    var sectionName = Encoding.ASCII.GetString(binReader.ReadBytes(8)).TrimEnd('\0');
                    var newPESI = new PESectionInfo()
                    {
                        Name = sectionName,
                        VirtualSize = binReader.ReadUInt32(),
                        VirtualAddress = binReader.ReadUInt32(),
                        PhysicalSize = binReader.ReadUInt32(),
                        PhysicalAddress = binReader.ReadUInt32()
                    };
                    newPESI.VirtualOffsetFromPhysical = newPESI.VirtualAddress - newPESI.PhysicalAddress;
                    sectionInfo.Add(newPESI);
                    baseFs.Position += 0x10;
                }
            }
            baseFs.Position = restorePos;
            if (sectionInfo == null)
                return null;
            sectionInfo.Sort((x, y) => x.PhysicalAddress.CompareTo(y.PhysicalAddress));
            return new BasicPEInfo(checksumPosition, sectionInfo);
        }

        internal PESectionInfo SectionForVA(long virtualAddress)
        {
            for (int i = Sections.Count - 1; i >= 0; i--)
            {
                if (Sections[i].VirtualAddress < virtualAddress)
                    return Sections[i];
            }
            return null;
        }

        internal long VirtualToPhysicalAddress(long virtualAddress)
        {
            return virtualAddress - SectionForVA(virtualAddress).VirtualOffsetFromPhysical;
        }
    }

    internal class PESectionInfo
    {
        internal string Name { get; set; }
        internal uint VirtualSize { get; set; }
        internal uint VirtualAddress { get; set; }
        internal uint PhysicalSize { get; set; }
        internal uint PhysicalAddress { get; set; }
        internal uint VirtualOffsetFromPhysical { get; set; }
    }
}
