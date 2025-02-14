using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace GameFramework
{
    public abstract class ExcelPipeline
    {
        public class ExcelContent
        {
            public DataSet dataSet;
            public string fileName;
            public string filePath;
            public string md5;
            public SheetExportType sheetExportType = SheetExportType.FullyDecentralized;
            public CellExportType cellExportType = CellExportType.FullyDecentralized;
            public Dictionary<string, SheetContent> sheetDic = new Dictionary<string, SheetContent>();
        }

        public class SheetContent
        {
            public DataTable dataTable;
            public string originalSheetName;
            public string sheetName;
            public string fileName;
            public string fieldName;
            public Dictionary<int, object> content = new Dictionary<int, object>();
        }

        public class ExportConfig
        {
            public string excelName;
            public SheetExportType sheetType;
            public CellExportType cellType;
        }

        /// <summary>
        /// 导出数据表方式（sheet完全分散导出、sheet部分合并导出、sheet完全合并导出）
        /// </summary>
        public enum SheetExportType
        {
            None,
            /// <summary>
            /// 完全分散导出（sheet完全分散导出：不同的脚本，不同的源文件）
            /// </summary>
            FullyDecentralized,
            /// <summary>
            /// 部分合并导出（sheet部分合并导出：同一个脚本，不同的源文件）
            /// </summary>
            PartialMerged,
            /// <summary>
            /// 完全合并导出（sheet完全合并导出：同一个脚本，同一个源文件）
            /// </summary>
            FullyMerged,
        }

        /// <summary>
        /// 单元格导出方式（）
        /// </summary>
        public enum CellExportType
        {
            None,
            /// <summary>
            /// 完全分散导出（每一列对应一个字段）
            /// </summary>
            FullyDecentralized,
            /// <summary>
            /// 列合并（所有列对应一个字典（Key:列的描述，Value:内容））
            /// </summary>
            ColumnMerge
        }

        [Serializable]
        public class TableBuildData
        {
            public string sourceDirectory;
            public string exportScriptDirectory;
            public string exportResourceDirectory;

            public static TableBuildData DefaultDataTableBuildData()
            {
                TableBuildData buildData = new TableBuildData();
                buildData.sourceDirectory = "../excel/Excel";
                buildData.exportResourceDirectory = "Assets/Resources/TableData";
                buildData.exportScriptDirectory = "Assets/Scripts/Game/TableData";
                return buildData;
            }

            public static TableBuildData DefaultTextTableBuildData()
            {
                TableBuildData buildData = new TableBuildData();
                buildData.sourceDirectory = "../../localization";
                buildData.exportResourceDirectory = "Assets/Resources/TextTable";
                buildData.exportScriptDirectory = "Assets/Scripts/Game/TextTable";
                return buildData;
            }
        }

        public static event Action Event_OnUpdateWillBegin;
        public static event Action Event_OnUpdateCompleted;

        //前4行定义(前四行有特殊含义,不是表示数据位)
        protected int MinRowCount = 6;
        protected int Row_TableData_Describe = 0;
        protected int Row_TableDataField_Describe = 1;
        protected int Row_TableDataField_Name = 2;
        protected int Row_TableDataField_Type = 4;
        protected int Column_Id = 0;
        protected int MinColumnCount = 1;

        protected readonly string[] Symbol_Stop = new string[] { "(", ")", "[", "]", "{", "}" };
        //当数据类型是交错数组时，这个列表元素的顺序就是数组拆分的顺序
        protected readonly char[] Symbol_Separate = new char[] { '|', ',', ';', ':' };
        //支持的数据类型
        protected readonly List<string> supportedDataTypes = new List<string>()
        {
            "int","ObscuredInt","long","ObscuredLong","float","ObscuredFloat","double","ObscuredDouble","bool","ObscuredBool","string","ObscuredString", "Vector2","Vector3", "IDCount", "IDIntValue", "IDFloatValue", "IDWeight", "IDWeightCount","RangeRandom","RangeWeight",
            "int[]","long[]","float[]","double[]","bool[]","string[]","Vector2[]","Vector3[]","IDCount[]", "IDIntValue[]", "IDFloatValue[]", "IDWeight[]", "IDWeightCount[]","RangeRandom[]","RangeWeight[]",
            "IDCount[][]","IDTripleValue","IDTripleValue[]","Article","Article[]","Range","Range[]","ItemRate","ItemRate[]","ItemRateFloat", "ItemRateFloat[]","AttributeValue", "AttributeValue[]",
        };

        //模板文件
        protected readonly string TemplateDirectory = "Assets/GameFramework/ExcelPipeline/ScriptTemplate/";
        protected readonly string TemplateFile_TableData = "TableData.txt";
        protected readonly string TemplateFile_TableData_ColumnMerge = "TableData_ColumnMerge.txt";
        protected readonly string TemplateFile_TableDataField = "TableDataField.txt";
        protected readonly string TemplateFile_TableDataSystem = "TableDataSystem.txt";
        protected readonly string TemplateFile_TableDataSystemContent = "TableDataSystemContent.txt";
        protected readonly string TemplateFile_TableDataSystemContent_PartialMergedExport = "TableDataSystemContent_PartialMergedExport.txt";

        protected readonly string FieldName = "#FieldName#";
        protected readonly string UpperCaseFieldName = "#UpperCaseFieldName#";
        protected readonly string TableDataName = "#TableDataName#";
        protected readonly string ManagerName = "#TableDataSystem#";

        //缓存配置文件
        protected readonly string Directory_PlayerPreferences = "../Library";

        //扩展名
        protected readonly string FileExtension_CS = ".cs";
        protected readonly string FileExtension_JSON = ".json";

        //Excel文件名前缀（T:测试数据表（补充主表内容）、C:子表（覆盖主表内容））
        protected readonly string ExcelPrefix_T = "T";
        protected readonly string ExcelPrefix_Add = "#";
        protected readonly string ExcelPrefix_C = "C";

        public TableBuildData buildData;
        protected List<string> excelPaths = new List<string>();
        protected Dictionary<string, ExcelContent> mainExcelContent = new Dictionary<string, ExcelContent>();
        protected Dictionary<string, ExcelContent> childExcelContent = new Dictionary<string, ExcelContent>();
        protected Dictionary<string, ExcelContent> testExcelContent = new Dictionary<string, ExcelContent>();
        protected Dictionary<string, ExportConfig> exportConfig = new Dictionary<string, ExportConfig>();
        protected Dictionary<string, string> localMD5Dic = new Dictionary<string, string>();

        protected abstract string SystemFileName { get; }
        protected abstract string FileName_ExportConfig { get; }

        public void UpdateExcelToJson()
        {
            Event_OnUpdateWillBegin?.Invoke();
            if (buildData == null)
            {
                LoadBuildData();
            }
            Debug.Log("开始更新表格");
            PrepareConfig();
            ConvertExcelToDataTable();
            UpdateTableDataScript();
            UpdateTableDataSystem();
            UpdateTableDataJson();
            UpdateCompleted();
            Event_OnUpdateCompleted?.Invoke();
        }

        #region 准备配置

        /// <summary>
        /// 准备相关配置数据
        /// </summary>
        protected virtual void PrepareConfig()
        {
            Debug.Log("1.准备相关配置数据");
            PrepareDirectory();
            PrepareExcelPath();
            PrepareExportConfig();
        }

        protected void PrepareDirectory()
        {
            if (Directory.Exists(buildData.exportScriptDirectory))
            {
                Directory.Delete(buildData.exportScriptDirectory, true);
            }
            if (Directory.Exists(buildData.exportResourceDirectory))
            {
                Directory.Delete(buildData.exportResourceDirectory, true);
            }

            if (Directory.Exists(buildData.exportScriptDirectory) == false)
            {
                Directory.CreateDirectory(buildData.exportScriptDirectory);
            }
            if (Directory.Exists(buildData.exportResourceDirectory) == false)
            {
                Directory.CreateDirectory(buildData.exportResourceDirectory);
            }
        }

        /// <summary>
        /// 获取所有表数据文件路径
        /// </summary>
        /// <returns></returns>
        protected void PrepareExcelPath()
        {
            if (Directory.Exists(buildData.sourceDirectory))
            {
                excelPaths.Clear();
                string[] files = Directory.GetFiles(buildData.sourceDirectory, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = files[i].Replace("\\", "/");
                    if ((fileName.EndsWith(".xls") || fileName.EndsWith(".xlsx") || fileName.EndsWith(".xlsm")) && fileName.Contains("~$") == false)
                    {
                        excelPaths.Add(fileName);
                    }
                }
                excelPaths.Sort((a, b) => a.CompareTo(b));
                if (excelPaths.Count <= 0)
                {
                    Debug.Log(string.Format("在{0}路径下未找到Excel文件,请检查", buildData.sourceDirectory));
                }
            }
            else
            {
                Debug.LogError(string.Format("{0}路径不存在,请检查", buildData.sourceDirectory));
            }
        }

        /// <summary>
        /// 获取数据表的导出类型配置
        /// </summary>
        protected void PrepareExportConfig()
        {
            exportConfig.Clear();
            string path = Path.Combine(buildData.sourceDirectory, FileName_ExportConfig);
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (var item in lines)
                {
                    string[] content = item.Split(Symbol_Separate);
                    if (content.Length >= 3 && int.TryParse(content[1], out int sheetType) && int.TryParse(content[2], out int cellType))
                    {
                        ExportConfig config = new ExportConfig();
                        config.excelName = content[0];
                        config.sheetType = (SheetExportType)sheetType;
                        config.cellType = (CellExportType)cellType;
                        exportConfig.Add(config.excelName, config);
                    }
                }
            }
        }

        #endregion

        #region 读取数据表

        /// <summary>
        /// 将excel读取到本地为DataTable文件
        /// </summary>
        protected virtual void ConvertExcelToDataTable()
        {
            mainExcelContent.Clear();
            testExcelContent.Clear();
            childExcelContent.Clear();
            Debug.Log("2.将excel读取到本地为DataTable文件");
            foreach (var filePath in excelPaths)
            {
                string directoryName = Path.GetFileName(Path.GetDirectoryName(filePath));
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                //FileStream excelStream = File.OpenRead(filePath);
                FileStream excelStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(excelStream);
                DataSet dataSet = excelReader.AsDataSet();
                System.Security.Cryptography.MD5 md5Computer = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] md5Array = md5Computer.ComputeHash(excelStream);
                string md5 = BitConverter.ToString(md5Array);
                excelStream.Dispose();
                excelStream.Close();
                ExcelContent excelContent = new ExcelContent();
                excelContent.dataSet = dataSet;
                excelContent.filePath = filePath;
                excelContent.fileName = FormatExcelName(fileName);
                excelContent.md5 = md5;
                excelContent.sheetExportType = SheetExportType.FullyDecentralized;
                //标记导出配置
                if (exportConfig.ContainsKey(fileName))
                {
                    excelContent.sheetExportType = exportConfig[fileName].sheetType;
                    excelContent.cellExportType = exportConfig[fileName].cellType;
                }
                PrepareExportType(excelContent);
                bool isTest = false;

                if (directoryName.StartsWith(ExcelPrefix_Add) || fileName.StartsWith(ExcelPrefix_Add))
                {
                    continue;
                }

                if (directoryName.StartsWith(ExcelPrefix_T) || fileName.StartsWith(ExcelPrefix_T))
                {
                    isTest = true;
                    testExcelContent.Add(excelContent.fileName, excelContent);
                }
                else if (directoryName.StartsWith(ExcelPrefix_C) || fileName.StartsWith(ExcelPrefix_C))
                {
                    childExcelContent.Add(excelContent.fileName, excelContent);
                    Debug.Log($"标记{filePath}为子数据表");
                }
                else
                {
                    mainExcelContent.Add(excelContent.fileName, excelContent);
                }

                if (excelContent.sheetExportType == SheetExportType.FullyMerged)
                {
                    for (int i = 0; i < dataSet.Tables.Count; i++)
                    {
                        DataTable dataTable = dataSet.Tables[i];
                        AddSheet(excelContent, dataTable, isTest, filePath);
                    }
                }
                else
                {
                    AddSheet(excelContent, dataSet.Tables[0], isTest, filePath);
                }
            }
        }

        protected virtual void PrepareExportType(ExcelContent excelContent)
        {
        }

        /// <summary>
        /// 生成TableData脚本文件
        /// </summary>
        protected virtual void AddSheet(ExcelContent excelContent, DataTable dataTable, bool isTest, string filePath)
        {
            if (dataTable.Rows.Count >= MinRowCount && dataTable.Columns.Count >= MinColumnCount)
            {
                try
                {
                    SheetContent sheet = new SheetContent();
                    string tableName = FormatTableName(dataTable.TableName);
                    sheet.dataTable = dataTable;
                    sheet.originalSheetName = dataTable.TableName;
                    sheet.sheetName = dataTable.TableName;
                    if (isTest)
                    {
                        sheet.fileName = $"{tableName}_Test";
                        sheet.fieldName = $"{tableName.Substring(0, 1).ToLower() + tableName.Substring(1)}_Test";
                    }
                    else
                    {
                        sheet.fileName = $"{tableName}";
                        sheet.fieldName = $"{tableName.Substring(0, 1).ToLower() + tableName.Substring(1)}";
                    }
                    excelContent.sheetDic.Add(sheet.sheetName, sheet);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{filePath}表中的Sheet: {dataTable.TableName}存在格式问题:{e}");
                }
            }
            else
            {
                Debug.LogWarning($"{filePath}表中的Sheet: {dataTable.TableName}存在格式问题。行数少于{MinRowCount}或列数少于{MinColumnCount}");
            }
        }

        #endregion

        #region 更新脚本文件

        /// <summary>
        /// 更新TableData.cs文件
        /// </summary>
        protected virtual void UpdateTableDataScript()
        {
            Debug.Log("3.更新TableData.cs文件");
            foreach (var excelContent in mainExcelContent)
            {
                foreach (var sheetContent in excelContent.Value.sheetDic)
                {
                    GenerateTableDataScript(excelContent.Value, sheetContent.Value);
                    //完全合并导出与部分合并导出，最终只会产生同一份脚本，不需要重复导出
                    if (excelContent.Value.sheetExportType != SheetExportType.FullyDecentralized)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 生成TableData脚本文件
        /// </summary>
        protected virtual void GenerateTableDataScript(ExcelContent excelContent, SheetContent sheetContent)
        {
            //获取表数据字段的模板文件并填充内容
            string fieldTemplate = GetTextAssetContent(Path.Combine(TemplateDirectory, TemplateFile_TableDataField));
            //获取字段内容
            StringBuilder fieldContent = new StringBuilder();
            string fieldType = string.Empty;
            for (int i = 0; i < sheetContent.dataTable.Columns.Count; i++)
            {
                string columnFieldType = sheetContent.dataTable.Rows[Row_TableDataField_Type][i].ToString();
                string fieldDescribe = sheetContent.dataTable.Rows[Row_TableDataField_Describe][i].ToString();
                string fieldName = sheetContent.dataTable.Rows[Row_TableDataField_Name][i].ToString();
                //没有填数据类型的列不导出或者字段名称以#开头
                if (string.IsNullOrEmpty(columnFieldType) == false && fieldDescribe.StartsWith("#") == false)
                {
                    fieldType = columnFieldType;
                    if (i != Column_Id)
                    {
                        fieldContent.AppendLine();
                        fieldContent.Append(string.Format(fieldTemplate, fieldDescribe, columnFieldType, fieldName));
                    }
                }
            }

            //获取TableData模板文件并填充表名与字段,创建出TableData.cs脚本
            string describe = excelContent.fileName;

            //默认单元格分散
            string tableDataContent;
            if (excelContent.cellExportType == CellExportType.ColumnMerge)
            {
                tableDataContent = GetTextAssetContent(Path.Combine(TemplateDirectory, TemplateFile_TableData_ColumnMerge));
                tableDataContent = string.Format(tableDataContent, describe, sheetContent.fileName, fieldType, fieldType);
            }
            else
            {
                tableDataContent = GetTextAssetContent(Path.Combine(TemplateDirectory, TemplateFile_TableData));
                tableDataContent = string.Format(tableDataContent, describe, sheetContent.fileName, fieldContent);
            }

            string exprotPath = Path.Combine(buildData.exportScriptDirectory, sheetContent.fileName + FileExtension_CS);
            File.WriteAllText(exprotPath, tableDataContent, new UTF8Encoding(true));
        }

        #endregion

        #region 更新管理文件

        /// <summary>
        /// 更新TableDataSystem.cs文件
        /// </summary>
        protected virtual void UpdateTableDataSystem()
        {
            Debug.Log("4.更新TableDataSystem.cs文件");

            StringBuilder loadMethodContent = new StringBuilder();
            StringBuilder unloadMethodContent = new StringBuilder();
            StringBuilder getMethodContent = new StringBuilder();

            foreach (var excelContent in mainExcelContent)
            {
                foreach (var sheetContent in excelContent.Value.sheetDic)
                {
                    if (excelContent.Value.sheetExportType == SheetExportType.PartialMerged)
                    {
                        string getMethodPath = Path.Combine(TemplateDirectory, TemplateFile_TableDataSystemContent_PartialMergedExport);
                        getMethodContent.AppendLine(GetTableDataSystemContent(getMethodPath, sheetContent.Value));
                        unloadMethodContent.AppendLine($"        {sheetContent.Value.fieldName}.Clear();");
                    }
                    else
                    {
                        string getMethodPath = Path.Combine(TemplateDirectory, TemplateFile_TableDataSystemContent);
                        getMethodContent.AppendLine(GetTableDataSystemContent(getMethodPath, sheetContent.Value));
                        loadMethodContent.AppendLine($"        Check{sheetContent.Value.fileName}();");
                        unloadMethodContent.AppendLine($"        {sheetContent.Value.fieldName} = null;");
                    }
                    //合并导出只读取第一个sheet
                    if (excelContent.Value.sheetExportType != SheetExportType.FullyDecentralized)
                    {
                        break;
                    }
                }
            }

            string templatePath = Path.Combine(TemplateDirectory, TemplateFile_TableDataSystem);
            string templateContent = (AssetDatabase.LoadAssetAtPath(templatePath, typeof(TextAsset)) as TextAsset).text;
            templateContent = templateContent.Replace(ManagerName, SystemFileName);
            string content = string.Format(templateContent, loadMethodContent, unloadMethodContent, getMethodContent);

            string scriptPath = Path.Combine(buildData.exportScriptDirectory, SystemFileName + FileExtension_CS);
            File.WriteAllText(scriptPath, content);
        }

        protected string GetTableDataSystemContent(string path, SheetContent sheet)
        {
            string templateContent = GetTextAssetContent(path);
            string result = templateContent.Replace(TableDataName, sheet.fileName);
            result = result.Replace(FieldName, sheet.fieldName);
            result = result.Replace(UpperCaseFieldName, sheet.fieldName.Substring(0,1).ToUpper() + sheet.fileName.Substring(1));
            return result;
        }

        #endregion

        #region 更新Json文件

        /// <summary>
        /// 更新TableData.json文件
        /// </summary>
        protected virtual void UpdateTableDataJson()
        {
            Debug.Log("5.更新TableData.json文件");
            UpdateMainTableDataJson();
            UpdateChildTableDataJson();
            UpdateTestTableDataJson();

            foreach (var excelContent in mainExcelContent)
            {
                ExportTableDataJson(excelContent.Value, buildData.exportResourceDirectory);
            }
            foreach (var excelContent in testExcelContent)
            {
                string path = Path.Combine(buildData.exportResourceDirectory, "Test");
                ExportTableDataJson(excelContent.Value, path);
            }
        }

        protected void UpdateMainTableDataJson()
        {
            foreach (var excelContent in mainExcelContent)
            {
                foreach (var sheetContent in excelContent.Value.sheetDic)
                {
                    ConvertSheetToJson(excelContent.Value, sheetContent.Value);
                }
            }
        }

        protected void UpdateChildTableDataJson()
        {
            foreach (var childExcel in childExcelContent)
            {
                if (mainExcelContent.TryGetValue(childExcel.Key, out ExcelContent mainExcel))
                {
                    foreach (var childSheet in childExcel.Value.sheetDic)
                    {
                        ConvertSheetToJson(childExcel.Value, childSheet.Value);
                        //测试表中只包含了测试内容，其余内容由主表补充
                        if (mainExcel.sheetDic.TryGetValue(childSheet.Key, out SheetContent mainSheet))
                        {
                            foreach (var item in childSheet.Value.content)
                            {
                                if (mainSheet.content.ContainsKey(item.Key))
                                {
                                    mainSheet.content[item.Key] = item.Value;
                                }
                                else
                                {
                                    mainSheet.content.Add(item.Key, item.Value);
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in childSheet.Value.content)
                            {
                                mainSheet.content.Add(item.Key, item.Value);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"{childExcel.Key}只存在子表，不存在主表；请检查！！！");
                }
            }
        }

        protected void UpdateTestTableDataJson()
        {
            foreach (var testExcel in testExcelContent)
            {
                if (mainExcelContent.TryGetValue(testExcel.Key, out ExcelContent mainExcel))
                {
                    foreach (var testSheet in testExcel.Value.sheetDic)
                    {
                        ConvertSheetToJson(testExcel.Value, testSheet.Value);
                        //测试表中只包含了测试内容，其余内容由主表补充
                        if (mainExcel.sheetDic.TryGetValue(testSheet.Key, out SheetContent mainSheet))
                        {
                            foreach (var item in mainSheet.content)
                            {
                                if (testSheet.Value.content.ContainsKey(item.Key) == false)
                                {
                                    testSheet.Value.content.Add(item.Key, item.Value);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError($"测试表：{testExcel.Key}中的sheet{testSheet.Key}，在主表中不存在；请检查！！！");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"{testExcel.Key}只存在测试表，不存在主表；请检查！！！");
                }
            }
        }

        protected virtual void ConvertSheetToJson(ExcelContent excelContent, SheetContent sheetContent)
        {
            for (int i = MinRowCount - 1; i < sheetContent.dataTable.Rows.Count; i++)
            {
                //完全分散
                Dictionary<object, object> decentralizedJson = new Dictionary<object, object>();
                Dictionary<object, object> templateJson = new Dictionary<object, object>();
                Dictionary<object, object> columnMergeJson = new Dictionary<object, object>();
                int id = -1;
                int j = 0;
                bool isBlankRow = false;
                for (; j < sheetContent.dataTable.Columns.Count; j++)
                {
                    string fieldType = sheetContent.dataTable.Rows[Row_TableDataField_Type][j].ToString();
                    string fieldDescribe = sheetContent.dataTable.Rows[Row_TableDataField_Describe][j].ToString();
                    if (string.IsNullOrEmpty(fieldType) == false && fieldDescribe.StartsWith("#") == false)
                    {
                        bool isAdditionalType = GameEditorConfig.IsAdditionalEnum(fieldType);
                        if (supportedDataTypes.Contains(fieldType) || isAdditionalType)
                        {
                            string key = sheetContent.dataTable.Rows[Row_TableDataField_Name][j].ToString();
                            //try
                            {
                                string valueString = sheetContent.dataTable.Rows[i][j].ToString();
                                if (j == Column_Id && string.IsNullOrEmpty(valueString))
                                {
                                    //ID列如果为空，整行视为空白行，跳过导出
                                    isBlankRow = true;
                                    break;
                                }
                                object value = GetFieldValue(fieldType, valueString, isAdditionalType);
                                decentralizedJson.Add(key, value);
                                //Id列需要特殊标记
                                if (j == Column_Id)
                                {
                                    id = int.Parse(value.ToString());
                                    columnMergeJson.Add(key, value);
                                }
                                else
                                {
                                    templateJson.Add(key, value);
                                }
                            }
                            //catch (Exception e)
                            //{
                            //    throw new UnityException($"数据表：{excelContent.filePath}中的Sheet：{sheetContent.originalSheetName}中的第{i + 1}行第{j + 1}列解析错误,请检查！错误内容：{e}");
                            //}
                        }
                        else
                        {
                            throw new UnityException($"数据表：{excelContent.filePath}中的Sheet：{sheetContent.originalSheetName}中的第{j + 1}列是未知的数据类型-{fieldType}-,请检查");
                        }
                    }
                }

                if (isBlankRow)
                {
                    continue;
                }

                //补充：数值出现了同表中出现了相同Id，因此加入异常捕获
                try
                {
                    if (excelContent.cellExportType == CellExportType.FullyDecentralized)
                    {
                        sheetContent.content.Add(id, decentralizedJson);
                    }
                    else if (excelContent.cellExportType == CellExportType.ColumnMerge)
                    {
                        columnMergeJson.Add("dataDic", templateJson);
                        sheetContent.content.Add(id, columnMergeJson);
                    }
                }
                catch (Exception e)
                {
                    throw new UnityException($"数据表：{excelContent.filePath}中的Sheet：{sheetContent.originalSheetName}中的第{i + 1}行第{j + 1}列出现异常请检查！错误内容：{e}");
                }
            }
        }

        protected virtual void ExportTableDataJson(ExcelContent excelContent, string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }
            string fileName = "";
            List<object> mergeContent = new List<object>();
            int index = 0;
            foreach (var sheetContent in excelContent.sheetDic)
            {
                List<object> contentList = new List<object>();
                foreach (var item in sheetContent.Value.content)
                {
                    contentList.Add(item.Value);
                }
                if (excelContent.sheetExportType != SheetExportType.FullyMerged)
                {
                    string relativePath = Path.Combine(path, sheetContent.Value.fileName + FileExtension_JSON);
                    string absolutePath = Path.GetFullPath(Path.Combine(Path.GetFullPath("."), relativePath));
                    File.WriteAllText(absolutePath, JsonConvert.SerializeObject(contentList, Formatting.Indented));
                }
                else
                {
                    mergeContent.AddRange(contentList);
                }
                if (index == 0)
                {
                    fileName = sheetContent.Value.fileName;
                }
                index++;
            }
            if (excelContent.sheetExportType == SheetExportType.FullyMerged)
            {
                string relativePath = Path.Combine(path, fileName + FileExtension_JSON);
                string absolutePath = Path.GetFullPath(Path.Combine(Path.GetFullPath("."), relativePath));
                string content = JsonConvert.SerializeObject(mergeContent, Formatting.Indented).Replace("\\\\", "\\");
                File.WriteAllText(absolutePath, content, Encoding.UTF8);
            }
        }

        #endregion

        #region 更新完成

        /// <summary>
        /// 更新完成
        /// </summary>
        protected virtual void UpdateCompleted()
        {
            Debug.Log("6.表格更新完成，请进入游戏查看");
            AssetDatabase.Refresh();
            FixLineEnding();
        }

        /// <summary>
        /// 纠正所有meta文件的line ending
        /// </summary>
        protected virtual void FixLineEnding()
        {
            var files = Directory.GetFiles(buildData.exportResourceDirectory, "*.meta");
            foreach (var file in files)
            {
                string fileContent = File.ReadAllText(file);
                fileContent = fileContent.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                File.WriteAllText(file, fileContent);
            }
            files = Directory.GetFiles(buildData.exportScriptDirectory, "*.meta");
            foreach (var file in files)
            {
                string fileContent = File.ReadAllText(file);
                fileContent = fileContent.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                File.WriteAllText(file, fileContent);
            }

            AssetDatabase.Refresh();
        }

        #endregion

        #region 辅助函数

        protected object GetFieldValue(string type, object value, bool isAdditionalType)
        {
            object result = null;
            string current = value.ToString();
            if (string.IsNullOrEmpty(current) == false)
            {
                if (type != "string")
                {
                    for (int i = 0; i < Symbol_Stop.Length; i++)
                    {
                        current = current.Replace(Symbol_Stop[i], "");
                    }
                }
                current = current.Trim();
            }

            if (type == "int" || type == "ObscuredInt" || isAdditionalType)
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = 0;
                }
                else
                {
                    result = int.Parse(current);
                }
            }
            else if (type == "long" || type == "ObscuredLong")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = 0;
                }
                else
                {
                    result = ulong.Parse(current);
                }
            }
            else if (type == "float" || type == "ObscuredFloat")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = 0;
                }
                else
                {
                    result = float.Parse(current);
                }
            }
            else if (type == "double" || type == "ObscuredDouble")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = 0;
                }
                else
                {
                    result = double.Parse(current);
                }
            }
            else if (type == "bool" || type == "ObscuredBool")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = false;
                }
                else
                {
                    result = Convert.ToBoolean(int.Parse(current));
                }
            }
            else if (type == "string")
            {
                result = current;
            }
            else if (type == "int[]" || type == "ObscuredInt[]" || type == "long[]" || type == "ObscuredLong[]" ||
                     type == "float[]" || type == "ObscuredFloat[]" || type == "double[]" || type == "ObscuredDouble[]" ||
                     type == "bool[]" || type == "ObscuredBool[]" || type == "string[]" || type == "ObscuredString[]")
            {
                if (type == "string[]" || type == "ObscuredString[]")
                {
                    string[] stringArray = current.Replace("\"", "").Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    result = stringArray;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    if (type == "int[]" || type == "ObscuredInt[]")
                    {
                        result = Array.ConvertAll(stringArray, s => int.Parse(s));
                    }
                    else if (type == "long[]" || type == "ObscuredLong[]")
                    {
                        result = Array.ConvertAll(stringArray, s => long.Parse(s));
                    }
                    else if (type == "float[]" || type == "ObscuredFloat[]")
                    {
                        result = Array.ConvertAll(stringArray, s => float.Parse(s));
                    }
                    else if (type == "double[]" || type == "ObscuredDouble[]")
                    {
                        result = Array.ConvertAll(stringArray, s => double.Parse(s));
                    }
                    else if (type == "bool[]" || type == "ObscuredBool[]")
                    {
                        int[] target = Array.ConvertAll(stringArray, s => int.Parse(s));
                        result = Array.ConvertAll(target, s => Convert.ToBoolean(s));
                    }
                }
            }
            else if (type == "Vector2")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = Vector2.zero;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    result = new Vector2(float.Parse(stringArray[0]), float.Parse(stringArray[1]));
                }
            }
            else if (type == "Vector2[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    Vector2[] target = new Vector2[stringArray.Length / 2];
                    for (int i = 0; i < target.Length; i++)
                    {
                        target[i] = new Vector2(float.Parse(stringArray[i * 2]), float.Parse(stringArray[i * 2 + 1]));
                    }
                    result = target;
                }
            }
            else if (type == "Vector3")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = Vector3.zero;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    result = new Vector3(float.Parse(stringArray[0]), float.Parse(stringArray[1]), float.Parse(stringArray[2]));
                }
            }
            else if (type == "Vector3[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    Vector3[] target = new Vector3[stringArray.Length / 3];
                    for (int i = 0; i < target.Length; i++)
                    {
                        target[i] = new Vector3(float.Parse(stringArray[i * 3]), float.Parse(stringArray[i * 3 + 1]), float.Parse(stringArray[i * 3 + 2]));
                    }
                    result = target;
                }
            }
            else if (type == "IDCount")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDCount idCount = new IDCount
                    {
                        id = int.Parse(stringArray[0]),
                        count = int.Parse(stringArray[1])
                    };
                    result = idCount;
                }
            }
            else if (type == "IDIntValue")
            {
                if (string.IsNullOrEmpty(current))
                {
                    IDIntValue idValue = new IDIntValue
                    {
                        id = 0,
                        value = 0
                    };
                    result = idValue;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDIntValue idValue = new IDIntValue
                    {
                        id = int.Parse(stringArray[0]),
                        value = int.Parse(stringArray[1])
                    };
                    result = idValue;
                }
            }
            else if (type == "IDFloatValue")
            {
                if (string.IsNullOrEmpty(current))
                {
                    IDFloatValue idValue = new IDFloatValue
                    {
                        id = 0,
                        value = 0
                    };
                    result = idValue;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDFloatValue idValue = new IDFloatValue
                    {
                        id = int.Parse(stringArray[0]),
                        value = float.Parse(stringArray[1])
                    };
                    result = idValue;
                }
            }
            else if (type == "IDTripleValue")
            {
                if (string.IsNullOrEmpty(current))
                {
                    IDTripleValue idValue = new IDTripleValue
                    {
                        id = 0,
                        value = new IDIntValue()
                        {
                            id = 0,
                            value = 0
                        }
                    };
                    result = idValue;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDTripleValue idValue = new IDTripleValue
                    {
                        id = int.Parse(stringArray[0]),
                        value = new IDIntValue()
                        {
                            id = int.Parse(stringArray[1]),
                            value = int.Parse(stringArray[2]),
                        }
                    };
                    result = idValue;
                }
            }
            else if (type == "IDTripleValue[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDTripleValue[] target = new IDTripleValue[stringArray.Length / 3];
                    for (var i = 0; i < target.Length; i++)
                    {
                        var iDIntValue = new IDIntValue()
                        {
                            id = int.Parse(stringArray[i * 3 + 1]),
                            value = int.Parse(stringArray[i * 3 + 2]),
                        };
                        target[i] = new IDTripleValue()
                        {
                            id = int.Parse(stringArray[i * 3]),
                            value = iDIntValue
                        };
                    }
                    result = target;
                }
            }
            else if (type == "IDWeight")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDWeight idWeight = new IDWeight
                    {
                        id = int.Parse(stringArray[0]),
                        weight = int.Parse(stringArray[1])
                    };
                    result = idWeight;
                }
            }
            else if (type == "IDWeightCount")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDWeightCount idWeightCount = new IDWeightCount
                    {
                        id = int.Parse(stringArray[0]),
                        weight = int.Parse(stringArray[1]),
                        count = int.Parse(stringArray[2])
                    };
                    result = idWeightCount;
                }
            }
            else if (type == "RangeRandom")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    float[] floatArray = Array.ConvertAll(stringArray, s => float.Parse(s));
                    RangeRandom range = new RangeRandom
                    {
                        min = floatArray[0],
                        max = floatArray[1]
                    };
                    result = range;
                }
            }
            else if (type == "RangeWeight")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    float[] floatArray = Array.ConvertAll(stringArray, s => float.Parse(s));
                    RangeWeight rangeWeight = new RangeWeight(floatArray[0], floatArray[1], (int)floatArray[2]);
                    result = rangeWeight;
                }
            }
            else if (type == "IDCount[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDCount[] idCountArray = new IDCount[stringArray.Length / 2];
                    for (int i = 0; i < idCountArray.Length; i++)
                    {
                        idCountArray[i] = new IDCount();
                        idCountArray[i].id = int.Parse(stringArray[i * 2]);
                        idCountArray[i].count = int.Parse(stringArray[i * 2 + 1]);
                    }
                    result = idCountArray;
                }
            }
            else if (type == "IDIntValue[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDIntValue[] idValueArray = new IDIntValue[stringArray.Length / 2];
                    for (int i = 0; i < idValueArray.Length; i++)
                    {
                        idValueArray[i] = new IDIntValue();
                        idValueArray[i].id = int.Parse(stringArray[i * 2]);
                        idValueArray[i].value = int.Parse(stringArray[i * 2 + 1]);
                    }
                    result = idValueArray;
                }
            }
            else if (type == "IDFloatValue[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDFloatValue[] idValueArray = new IDFloatValue[stringArray.Length / 2];
                    for (int i = 0; i < idValueArray.Length; i++)
                    {
                        idValueArray[i] = new IDFloatValue();
                        idValueArray[i].id = int.Parse(stringArray[i * 2]);
                        idValueArray[i].value = float.Parse(stringArray[i * 2 + 1]);
                    }
                    result = idValueArray;
                }
            }
            else if (type == "IDWeight[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDWeight[] idWeightArray = new IDWeight[stringArray.Length / 2];
                    for (int i = 0; i < idWeightArray.Length; i++)
                    {
                        idWeightArray[i] = new IDWeight();
                        idWeightArray[i].id = int.Parse(stringArray[i * 2]);
                        idWeightArray[i].weight = int.Parse(stringArray[i * 2 + 1]);
                    }
                    result = idWeightArray;
                }
            }
            else if (type == "IDWeightCount[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    IDWeightCount[] idWeightCountArray = new IDWeightCount[stringArray.Length / 2];
                    for (int i = 0; i < idWeightCountArray.Length; i++)
                    {
                        idWeightCountArray[i] = new IDWeightCount();
                        idWeightCountArray[i].id = int.Parse(stringArray[i * 2]);
                        idWeightCountArray[i].weight = int.Parse(stringArray[i * 2 + 1]);
                        idWeightCountArray[i].count = int.Parse(stringArray[i * 2 + 2]);
                    }
                    result = idWeightCountArray;
                }
            }
            else if (type == "RangeRandom[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    RangeRandom[] articleArray = new RangeRandom[stringArray.Length / 2];
                    for (int i = 0; i < articleArray.Length; i++)
                    {
                        articleArray[i] = new RangeRandom
                        {
                            min = int.Parse(stringArray[i * 2]),
                            max = int.Parse(stringArray[i * 2 + 1])
                        };
                    }
                    result = articleArray;
                }
            }
            else if (type == "RangeWeight[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    float[] floatArray = Array.ConvertAll(stringArray, s => float.Parse(s));
                    var groupByCount = 3;
                    RangeWeight[] array = new RangeWeight[stringArray.Length / groupByCount];
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new RangeWeight(floatArray[i * groupByCount], floatArray[i * groupByCount + 1], (int)floatArray[i * groupByCount + groupByCount - 1]);
                    }
                    result = array;
                }
            }
            else if (type == "IDCount[][]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    char firstChar = char.MinValue;
                    foreach (var sepChar in Symbol_Separate)
                    {
                        if (current.Contains(sepChar))
                        {
                            if (firstChar == char.MinValue)
                            {
                                firstChar = sepChar;
                                break;
                            }
                        }
                    }
                    string[] firstStringArray = current.Split(firstChar, StringSplitOptions.RemoveEmptyEntries);
                    IDCount[][] firstArray = new IDCount[firstStringArray.Length][];
                    for (int i = 0; i < firstArray.GetLength(0); i++)
                    {
                        string[] secondStringArray = firstStringArray[i].Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                        var secondArray = new IDCount[secondStringArray.Length / 2];
                        for (int j = 0; j < secondArray.Length; j++)
                        {
                            var idCount = new IDCount(int.Parse(secondStringArray[j * 2]), int.Parse(secondStringArray[j * 2 + 1]));
                            secondArray[j] = idCount;
                        }
                        firstArray[i] = secondArray;
                    }
                    result = firstArray;
                }
            }            
            else if (type == "Article")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    Article article = new Article
                    {
                        id = int.Parse(stringArray[0]),
                        count = int.Parse(stringArray[1])
                    };
                    result = article;
                }
            }
            else if (type == "AttributeValue")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    AttributeValue attribute = new AttributeValue
                    {
                        ID = int.Parse(stringArray[0]),
                        attribute = float.Parse(stringArray[1])
                    };
                    result = attribute;
                }
            }
            else if (type == "Article[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    Article[] articleArray = new Article[stringArray.Length / 2];
                    for (int i = 0; i < articleArray.Length; i++)
                    {
                        articleArray[i] = new Article();
                        articleArray[i].id = int.Parse(stringArray[i * 2]);
                        articleArray[i].count = int.Parse(stringArray[i * 2 + 1]);
                    }
                    result = articleArray;
                }
            }
            else if (type == "ItemRate")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    ItemRate itemRate = new ItemRate
                    {
                        id = int.Parse(stringArray[0]),
                        weights = int.Parse(stringArray[1])
                    };
                    result = itemRate;
                }
            }
            else if (type == "ItemRate[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    ItemRate[] itemRateArray = new ItemRate[stringArray.Length / 2];
                    for (int i = 0; i < itemRateArray.Length; i++)
                    {
                        itemRateArray[i] = new ItemRate();
                        itemRateArray[i].id = int.Parse(stringArray[i * 2]);
                        itemRateArray[i].weights = int.Parse(stringArray[i * 2 + 1]);
                    }
                    result = itemRateArray;
                }
            }
            else if (type == "ItemRateFloat")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    ItemRateFloat itemRateFloat = new ItemRateFloat
                    {
                        id = int.Parse(stringArray[0]),
                        rate = float.Parse(stringArray[1])
                    };
                    result = itemRateFloat;
                }
            }
            else if (type == "Range")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    float[] floatArray = Array.ConvertAll(stringArray, s => float.Parse(s));
                    Range range = new Range
                    {
                        min = floatArray[0],
                        max = floatArray[1]
                    };
                    result = range;
                }
            }
            else if (type == "Range[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    Range[] articleArray = new Range[stringArray.Length / 2];
                    for (int i = 0; i < articleArray.Length; i++)
                    {
                        articleArray[i] = new Range
                        {
                            min = float.Parse(stringArray[i * 2]),
                            max = float.Parse(stringArray[i * 2 + 1])
                        };
                    }
                    result = articleArray;
                }
            }
            else if (type == "AttributeValue[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    AttributeValue[] taolitemRateFArray = new AttributeValue[stringArray.Length / 2];
                    for (int i = 0; i < taolitemRateFArray.Length; i++)
                    {
                        taolitemRateFArray[i] = new AttributeValue();
                        taolitemRateFArray[i].ID = int.Parse(stringArray[i * 2]);
                        taolitemRateFArray[i].attribute = float.Parse(stringArray[i * 2 + 1]);
                    }
                    result = taolitemRateFArray;
                }
            }
            else if (type == "ItemRateFloat[]")
            {
                if (string.IsNullOrEmpty(current))
                {
                    result = null;
                }
                else
                {
                    string[] stringArray = current.Split(Symbol_Separate, StringSplitOptions.RemoveEmptyEntries);
                    ItemRateFloat[] taolitemRateFArray = new ItemRateFloat[stringArray.Length / 2];
                    for (int i = 0; i < taolitemRateFArray.Length; i++)
                    {
                        taolitemRateFArray[i] = new ItemRateFloat();
                        taolitemRateFArray[i].id = int.Parse(stringArray[i * 2]);
                        taolitemRateFArray[i].rate = float.Parse(stringArray[i * 2 + 1]);
                    }
                    result = taolitemRateFArray;
                }
            }
            else
            {
                throw new UnityException(string.Format("支持类型{0},但未写代码", type));
            }
            return result;
        }

        protected string GetTextAssetContent(string filePath)
        {
            return (AssetDatabase.LoadAssetAtPath(filePath, typeof(TextAsset)) as TextAsset).text;
        }

        protected string FormatExcelName(string fileName)
        {
            return fileName.Replace(ExcelPrefix_T, "").Replace(ExcelPrefix_C, "");
        }

        protected string FormatTableName(string tableName)
        {
            //数据表的导出支持多个Sheet对应多个类，但是如果多个Sheet使用相同格式：“相同前缀-编号”，将使用“相同前缀”做为共同的类名
            string[] split = tableName.Split(Symbol_Separate);
            string name = split[0];
            if (name.Contains("_") == false && name.EndsWith("TableData") == false)
            {
                name += "TableData";
            }
            return name;
        }

        protected abstract void LoadBuildData();

        #endregion
    }
}