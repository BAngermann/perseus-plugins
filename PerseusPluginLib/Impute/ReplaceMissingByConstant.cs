using System.Drawing;
using BasicLib.Param;
using BasicLib.Util;
using PerseusApi;

namespace PerseusPluginLib.Impute{
	public class ReplaceMissingByConstant : IMatrixProcessing{
		public bool HasButton { get { return false; } }
		public Image ButtonImage { get { return null; } }
		public string HelpDescription { get { return "Replaces all missing values in expression columns with a constant."; } }
		public string HelpOutput { get { return "Same matrix but with missing values replaced."; } }
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }
		public string Name { get { return "Replace missing values by constant"; } }
		public string Heading { get { return "Imputation"; } }
		public bool IsActive { get { return true; } }
		public float DisplayOrder { get { return 1; } }
		public HelpType HelpDescriptionType { get { return HelpType.PlainText; } }
		public HelpType HelpOutputType { get { return HelpType.PlainText; } }
		public HelpType[] HelpSupplTablesType { get { return new HelpType[0]; } }

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ProcessInfo processInfo){
			float value = (float) param.GetDoubleParam("Value").Value;
			ReplaceMissingsByVal(value, mdata);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]
				{new DoubleParam("Value", 0){Help = "The value that is going to be filled in for missing values."}});
		}

		public static void ReplaceMissingsByVal(float value, IMatrixData data){
			for (int i = 0; i < data.RowCount; i++){
				for (int j = 0; j < data.ExpressionColumnCount; j++){
					if (float.IsNaN(data[i, j])){
						data[i, j] = value;
						data.IsImputed[i, j] = true;
					}
				}
			}
		}
	}
}