﻿using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CxAPI_Store.dto;

namespace CxAPI_Store
{
    class csvHelper
    {
        public int writeCVSFile(List<object> objList, resultClass token)
        {
            try
            {
                using (var writer = new StreamWriter(token.file_path + token.os_path + token.file_name))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(objList);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                return -1;
            }
            return 0;
        }

    }
}
