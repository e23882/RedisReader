using System;

namespace MahAppBase
{
    public class KeyListData : ICloneable
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public object Clone()
        {
            return new KeyListData
            {
                Name = this.Name,
                Type = this.Type
            };
        }
    }
}
