using System;

namespace EfCoreExtensions.RelationalUpdate
{
    public class RelationalUpdateConfigurationType
    {
        public RelationalUpdateConfigurationType()
        {

        }

        public RelationalUpdateConfigurationType(Type type)
        {
            Type = type;
        }

        public RelationalUpdateConfigurationType(Type type, bool removeDataInDatabase) : this(type)
        {
            RemoveDataInDatabase = removeDataInDatabase;
        }
        public Type Type { get; set; }
        public bool RemoveDataInDatabase { get; set; }
    }
}