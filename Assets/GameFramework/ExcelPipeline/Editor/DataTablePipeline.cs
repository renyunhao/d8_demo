namespace GameFramework
{
    public class DataTablePipeline : ExcelPipeline
    {
        protected override string SystemFileName => "TableSystem";
        protected override string FileName_ExportConfig => "export_config.txt";

        protected override void LoadBuildData()
        {
            buildData = GameEditorConfig.DataTableConfig;
        }
    }
}
