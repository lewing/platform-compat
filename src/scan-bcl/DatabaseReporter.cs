using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.DotNet.Cci;
using Microsoft.DotNet.Csv;
using Microsoft.DotNet.Scanner;

namespace scan_bcl
{
    internal sealed class DatabaseReporter : IExceptionReporter
    {
        private readonly Database _database;
        private readonly string _api;

        public DatabaseReporter(Database database, string api) {
            _database = database;
            _api = api;
        }

        public void Report(ExceptionInfo result, ITypeDefinitionMember member) {
            WriteMember(result, member);
        }

        public bool Query(string DocId) {
            return _database.Contains(DocId);
        }

        private void WriteMember(ExceptionInfo result, ITypeDefinitionMember member) {
            if (!result.Throws)
                return;

            if (!Query(member.DocId())) {
                _database.Add(member.DocId(), member.GetNamespaceName(), 
                        member.GetTypeName(), member.GetMemberSignature(), 
                        _api, result.Level.ToString());
            } else {
                _database.Remove(member.DocId());
            }
            
        }
    }
}