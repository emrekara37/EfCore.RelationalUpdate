using System;
using System.Collections.Generic;

namespace EfCoreExtensions.RelationalUpdate
{
    public class RelationalUpdateConfiguration
    {
        public RelationalUpdateConfiguration()
        {
            UpdatedTypes = new List<RelationalUpdateConfigurationType>();
            TriggerSaveChanges = true;
        }

        public RelationalUpdateConfiguration(bool triggerSaveChanges) :this()
        {
            TriggerSaveChanges = triggerSaveChanges;
        }
        public bool RemoveDataInDatabase { get; set; }
        public bool TriggerSaveChanges { get; set; }
        public List<RelationalUpdateConfigurationType> UpdatedTypes { get; set; }
        public RelationalUpdateConfiguration AddType(Type updatedType)
        {
            AddType(updatedType, RemoveDataInDatabase);
            return this;
        }
        public RelationalUpdateConfiguration AddType(Type updatedType, bool removeDataInDatabase)
        {
            UpdatedTypes.Add(new RelationalUpdateConfigurationType(updatedType, removeDataInDatabase));
            return this;
        }

    }
}