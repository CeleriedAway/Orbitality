using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace TWL {

    public partial class ZergRandom : IUpdatableFrom<TWL.ZergRandom>, IHashable, ICompareChechable<TWL.ZergRandom>, IJsonSerializable
    {
        public virtual void UpdateFrom(TWL.ZergRandom other) 
        {
            var SeedArrayCount = other.SeedArray.Length;
            var SeedArrayTemp = SeedArray;
            Array.Resize(ref SeedArrayTemp, SeedArrayCount);
            SeedArray = SeedArrayTemp;
            SeedArray.UpdateFrom(other.SeedArray);
            inext = other.inext;
            inextp = other.inextp;
        }
        public virtual void Deserialize(BinaryReader reader) 
        {
            SeedArray = reader.ReadSystem_Int32Array();
            inext = reader.ReadInt32();
            inextp = reader.ReadInt32();
        }
        public virtual void Serialize(BinaryWriter writer) 
        {
            SeedArray.Serialize(writer);
            writer.Write(inext);
            writer.Write(inextp);
        }
        public virtual ulong CalculateHash() 
        {
            System.UInt64 hash = 345093625;
            hash += (ulong)714987095;
            hash += hash << 11; hash ^= hash >> 7;
            hash += SeedArray.CalculateHash();
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inext;
            hash += hash << 11; hash ^= hash >> 7;
            hash += (System.UInt64)inextp;
            hash += hash << 11; hash ^= hash >> 7;
            return hash;
        }
        public virtual void CompareCheck(TWL.ZergRandom other, Stack<string> path) 
        {
            path.Push("SeedArray");
            SeedArray.CompareCheck(other.SeedArray, path);
            path.Pop();
            if (inext != other.inext) SerializationTools.LogCompError(path, "inext", other.inext, inext);
            if (inextp != other.inextp) SerializationTools.LogCompError(path, "inextp", other.inextp, inextp);
        }
        public virtual void ReadFromJsonField(JsonTextReader reader, string name) 
        {
            switch(name)
            {
                case "SeedArray":
                SeedArray = SeedArray.ReadFromJson(reader);
                break;
                case "inext":
                inext = (int)(Int64)reader.Value;
                break;
                case "inextp":
                inextp = (int)(Int64)reader.Value;
                break;
            }
        }
        public virtual void WriteJsonFields(JsonTextWriter writer) 
        {
            writer.WritePropertyName("SeedArray");
            SeedArray.WriteJson(writer);
            writer.WritePropertyName("inext");
            writer.WriteValue(inext);
            writer.WritePropertyName("inextp");
            writer.WriteValue(inextp);
        }
    }
}
#endif
