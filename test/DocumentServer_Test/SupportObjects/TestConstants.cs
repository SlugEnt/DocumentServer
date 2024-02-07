using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer_Test.SupportObjects
{
    public static class TestConstants
    {
        public const string FOLDER_TEST           = @"test";
        public const string FOLDER_TEST_PRIMARY   = @"test\primary";
        public const string FOLDER_TEST_SECONDARY = @"test\secondary";

        public const string FOLDER_PROD           = @"prod";
        public const string FOLDER_PROD_PRIMARY   = @"prod\primary";
        public const string FOLDER_PROD_SECONDARY = @"prod\secondary";

        // Storage Node Names
        public const string STORAGE_NODE_TEST_A = "NodeA";
        public const string STORAGE_NODE_TEST_B = "NodeB";
        public const string STORAGE_NODE_PROD_X = "NodeX";
        public const string STORAGE_NODE_PROD_Y = "NodeY";

        // Document Type Names
        public const string DOCTYPE_TEST_A = "DocA";
        public const string DOCTYPE_TEST_B = "DocB";
        public const string DOCTYPE_TEST_C = "DocC";
        public const string DOCTYPE_PROD_X = "DocX";
        public const string DOCTYPE_PROD_Y = "DocY";
        public const string DOCTYPE_PROD_Z = "DocZ";
    }
}