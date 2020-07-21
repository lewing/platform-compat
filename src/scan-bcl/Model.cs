using System;
using System.Collections.Generic;
using CsvHelper;

namespace scan_bcl
{
    internal sealed class DatabaseEntry
    {
        public DatabaseEntry(string DocId, string Namespace, string Type, string Member, string Api, string Nesting)
        {
            this.DocId = DocId;
            this.Namespace = Namespace;
            this.Type = Type;
            this.Member = Member;
            this.Api = Api;
            this.Nesting = Nesting;
        }

        public string DocId { get; set; }  
        public string Namespace { get; set; } 
        public string Type { get; set; } 
        public string Member { get; set; }
        public string Api { get; set; } 
        public string Nesting { get; set; }  
    }

    internal sealed class Database 
    {
        private readonly Dictionary<string, DatabaseEntry> _entries = new Dictionary<string, DatabaseEntry>();
        
        public void Add(string DocId, string Namespace, string Type, string Member, string Api, string Nesting)
        {
            var entry = new DatabaseEntry(DocId, Namespace, Type, Member, Api, Nesting);
            _entries.Add(DocId, entry);
        }

        public void Remove(string DocId)
        {
            _entries.Remove(DocId);
        }

        public bool Contains(string DocId)
        {
            return _entries.ContainsKey(DocId);
        }

        public IEnumerable<DatabaseEntry> Entries => _entries.Values;
    }
        
    
}