﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iesi.Collections.Generic;

namespace NHibernate.Envers.Configuration.Metadata
{
    /**
     * A register of all audit entity names used so far.
     * @author Simon Duduica, port of Envers Tools class by Adam Warski (adam at warski dot org)
     */
    public class AuditEntityNameRegister {
        private readonly ISet<String> auditEntityNames = new Iesi.Collections.Generic.HashedSet<String>();

        /**
         * @param auditEntityName Name of the audit entity.
         * @return True if the given audit entity name is already used.
         */
        private bool check(String auditEntityName) {
            return auditEntityNames.Contains(auditEntityName);
        }

        /**
         * Register an audit entity name. If the name is already registered, an exception is thrown.
         * @param auditEntityName Name of the audit entity.
         */
        public void register(String auditEntityName) {
            if (auditEntityNames.Contains(auditEntityName)) {
                throw new MappingException("The audit entity name '" + auditEntityName + "' is already registered.");
            }
            
            auditEntityNames.Add(auditEntityName);
        }

        /**
         * Creates a unique (not yet registered) audit entity name by appending consecutive numbers to the base
         * name. If the base name is not yet used, it is returned unmodified.
         * @param baseAuditEntityName The base entity name.
         * @return 
         */
        public String createUnique(String baseAuditEntityName) {
            String auditEntityName = baseAuditEntityName;
            int count = 1;
            while (check(auditEntityName)) {
                auditEntityName = baseAuditEntityName + count++;
            }

            return auditEntityName;
        }
    }
}
