namespace GameFramework
{
    /// <summary>
    /// 文本表生产线(多个表格,多个sheet合并)
    /// </summary>
    public class TextTablePipeline : ExcelPipeline
    {
        private const string TableData_FileName = "TextTableData";
        private const string TableData_FieldName = "textData";
        private const string DataTable_FileName = "文本表";

        protected override string SystemFileName => "TextSystem";
        protected override string FileName_ExportConfig => "";

        protected override void PrepareConfig()
        {
            base.PrepareConfig();
            MinRowCount = 4;
            Row_TableDataField_Name = 1;
            Row_TableDataField_Type = 2;
        }

        protected override void ConvertExcelToDataTable()
        {
            base.ConvertExcelToDataTable();
            foreach (var excelContent in mainExcelContent)
            {
                if (string.Equals(excelContent.Key, DataTable_FileName) == false)
                {
                    childExcelContent.Add(excelContent.Key, excelContent.Value);
                }
                else
                {
                    foreach (var item in excelContent.Value.sheetDic)
                    {
                        item.Value.sheetName = TableData_FileName;
                        item.Value.fileName = TableData_FileName;
                        item.Value.fieldName = TableData_FieldName;
                    }
                }
            }
            foreach (var item in childExcelContent)
            {
                if (mainExcelContent.ContainsKey(item.Key))
                {
                    mainExcelContent.Remove(item.Key);
                }
            }
        }

        protected override void PrepareExportType(ExcelContent excelContent)
        {
            excelContent.cellExportType = CellExportType.ColumnMerge;
            excelContent.sheetExportType = SheetExportType.FullyMerged;
        }

        protected override void LoadBuildData()
        {
            buildData = GameEditorConfig.TextTableConfig;
        }
    }
}
