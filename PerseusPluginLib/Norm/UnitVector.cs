using System;
using System.Drawing;
using BasicLib.Param;
using BasicLib.Util;
using PerseusApi;

namespace PerseusPluginLib.Norm{
	public class UnitVector : IMatrixProcessing{
		public bool HasButton { get { return false; } }
		public Image ButtonImage { get { return null; } }
		public string HelpDescription{
			get{
				return
					"The rows/columns are regarded as high-dimensional vectors. They are divided by their lengts resulting in a matrix of unit vectors.";
			}
		}
		public string HelpOutput { get { return "Normalized expression matrix."; } }
		public HelpType HelpDescriptionType { get { return HelpType.PlainText; } }
		public HelpType HelpOutputType { get { return HelpType.PlainText; } }
		public HelpType[] HelpSupplTablesType { get { return new HelpType[0]; } }
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ProcessInfo processInfo){
			SingleChoiceParam access = param.GetSingleChoiceParam("Matrix access");
			bool rows = access.Value == 0;
			UnitVectors(rows, mdata);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Matrix access"){
						Values = new[]{"Rows", "Columns"},
						Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
					}
				});
		}

		public void UnitVectors(bool rows, IMatrixData data){
			if (rows){
				for (int i = 0; i < data.RowCount; i++){
					double len = 0;
					for (int j = 0; j < data.ExpressionColumnCount; j++){
						double q = data[i, j];
						len += q*q;
					}
					len = Math.Sqrt(len);
					for (int j = 0; j < data.ExpressionColumnCount; j++){
						data[i, j] /= (float) len;
					}
				}
			} else{
				for (int j = 0; j < data.ExpressionColumnCount; j++){
					double len = 0;
					for (int i = 0; i < data.RowCount; i++){
						double q = data[i, j];
						len += q*q;
					}
					len = Math.Sqrt(len);
					for (int i = 0; i < data.RowCount; i++){
						data[i, j] /= (float) len;
					}
				}
			}
		}

		public string Name { get { return "Unit vectors"; } }
		public string Heading { get { return "Normalization"; } }
		public bool IsActive { get { return true; } }
		public float DisplayOrder { get { return -8; } }
	}
}