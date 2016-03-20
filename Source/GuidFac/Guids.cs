// Guids.cs
// MUST match guids.h
using System;

namespace PrivateDeveloperInc.GuidFac
{
    static class GuidList
    {
        public const string guidGuidFacPkgString = "06b6528b-1a8f-43fd-b717-411ea0dbb3de";
        public const string guidGuidFacCmdSetString = "deb2f181-665e-4cc2-b28e-ac82dc374e30";

        public static readonly Guid guidGuidFacCmdSet = new Guid(guidGuidFacCmdSetString);
    };
}