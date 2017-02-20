﻿using System;
using System.Collections.Generic;
using System.Text;
using static LanguageExt.Prelude;
using static LanguageExt.Process;
using LanguageExt;
using LanguageExt.Client;

namespace LanguageExt.Client
{
    class BarParse
    {
        const string header = "procsys:";
        public readonly string Source;
        public readonly int Length;
        public int Pos;

        StringBuilder sb = new StringBuilder();

        public BarParse(string source)
        {
            Source = source;
            Length = Source.Length;
        }

        public Either<string, Unit> Header()
        {
            Pos = 0;
            if (!Source.StartsWith(header)) return "Invalid request";
            Pos += header.Length;
            return unit;
        }

        public string GetNextText()
        {
            sb.Length = 0;

            for (; Pos < Length; Pos++)
            {
                var c = Source[Pos];
                if (c == '|')
                {
                    Pos++;
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public Either<string, string> GetNext()
        {
            var res = GetNextText();
            return String.IsNullOrEmpty(res)
                ? Left<string, string>("Empty field")
                : Right<string, string>(res);
        }

        public Either<string, long> GetLong() =>
            from x in GetNext()
            from y in parseLong(x).ToEither("Invalid long int")
            select y;

        public Either<string, ProcessId> GetProcessId() =>
            from x in GetNext()
            from y in FixupPID(x)
            select y;

        public Either<string, ClientMessageId> GetMessageId() =>
            GetLong().Map(ClientMessageId.New);

        public Either<string, ClientConnectionId> GetConnectionId() =>
            GetNext().Map(ClientConnectionId.New);

        Either<string, ProcessId> FixupPID(string pidStr) =>
            String.IsNullOrWhiteSpace(pidStr) || pidStr == "/no-sender"
                ? Right<string, ProcessId>(ProcessId.None)
                : ProcessId.TryParse(pidStr).MapLeft(ex => ex.Message);
    }
}
