using System.Collections.Generic;
using System.Drawing;
using BasicLib.Num;
using BasicLib.Param;
using BasicLib.Util;
using PerseusApi;
using PerseusPluginLib.Properties;

namespace PerseusPluginLib.Basic{
	public class DensityEstimationProcessing : IMatrixProcessing{
		public string Name { get { return "Density estimation"; } }
		public float DisplayOrder { get { return -3; } }
		public bool IsActive { get { return true; } }
		public bool HasButton { get { return true; } }
		public Image ButtonImage { get { return Resources.density_Image; } }
		public string Heading { get { return "Basic"; } }
		public string HelpDescription{
			get{
				return "The density of data points in two dimensions is calculated. Each data point is smoothed out" +
					" by a suitable Gaussian kernel.";
			}
		}
		public string HelpOutput{
			get{
				return "Depending on the 'Mode' parameter the output is either a copy of the input matrix with two numerical" +
					" columns added containing the density information, or a new matrix in which the expression values" +
					" represent the third dimension of density values.";
			}
		}
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }
		public HelpType HelpDescriptionType { get { return HelpType.PlainText; } }
		public HelpType HelpOutputType { get { return HelpType.PlainText; } }
		public HelpType[] HelpSupplTablesType { get { return new HelpType[0]; } }

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ProcessInfo processInfo){
			int[] colIndx = param.GetMultiChoiceParam("Column 1").Value;
			int[] colIndy = param.GetMultiChoiceParam("Column 2").Value;
			if (colIndx.Length == 0){
				processInfo.ErrString = "Please select some columns";
				return;
			}
			if (colIndx.Length != colIndy.Length){
				processInfo.ErrString = "Please select the same number of columns in the boxes for the first and second columns.";
				return;
			}
			int points = param.GetIntParam("Number of points").Value;
			for (int k = 0; k < colIndx.Length; k++){
				float[] xvals = GetColumn(mdata, colIndx[k]);
				float[] yvals = GetColumn(mdata, colIndy[k]);
				float[] xvals1;
				float[] yvals1;
				NumUtils.GetValidPairs(xvals, yvals, out xvals1, out yvals1);
				double xmin;
				double xmax;
				double ymin;
				double ymax;
				DensityEstimation.CalcRanges(xvals1, yvals1, out xmin, out xmax, out ymin, out ymax);
				float[,] values = DensityEstimation.GetValuesOnGrid(xvals1, xmin, (xmax - xmin) / points, points, yvals1, ymin,
					(ymax - ymin)/points, points);
				DensityEstimation.DivideByMaximum(values);
				//if (modeInd == 1){
				//	values = InvertRows(values);
				//	List<string> colNames = new List<string>();
				//	for (int i = 0; i < values.GetLength(1); i++){
				//		colNames.Add("" + i);
				//	}
				//	mdata.SetData(mdata.Name, colNames, values, new List<string>(), new List<string[]>(), new List<string>(),
				//		new List<string[][]>(), new List<string>(), new List<double[]>(), new List<string>(), new List<double[][]>());
				//	return;
				//}
				double[] xmat = new double[points];
				for (int i = 0; i < points; i++){
					xmat[i] = xmin + i*(xmax - xmin)/points;
				}
				double[] ymat = new double[points];
				for (int i = 0; i < points; i++){
					ymat[i] = ymin + i*(ymax - ymin)/points;
				}
				float[,] percvalues = CalcExcludedPercentage(values);
				double[] dvals = new double[xvals.Length];
				double[] pvals = new double[xvals.Length];
				for (int i = 0; i < dvals.Length; i++){
					double xx = xvals[i];
					double yy = yvals[i];
					if (!double.IsNaN(xx) && !double.IsNaN(yy)){
						int xind = ArrayUtils.ClosestIndex(xmat, xx);
						int yind = ArrayUtils.ClosestIndex(ymat, yy);
						dvals[i] = values[xind, yind];
						pvals[i] = percvalues[xind, yind];
					} else{
						dvals[i] = double.NaN;
						pvals[i] = double.NaN;
					}
				}
				string xname = GetColumnName(mdata, colIndx[k]);
				string yname = GetColumnName(mdata, colIndy[k]);
				mdata.AddNumericColumn("Density_" + xname + "_" + yname,
					"Density of data points in the plane spanned by the columns " + xname + " and " + yname + ".", dvals);
				mdata.AddNumericColumn("Excluded fraction_" + xname + "_" + yname,
					"Percentage of points with a point density smaller than at this point in the plane spanned by the columns " + xname +
						" and " + yname + ".", pvals);
			}
		}

		private static float[] GetColumn(IMatrixData matrixData, int ind){
			if (ind < matrixData.ExpressionColumnCount){
				return matrixData.GetExpressionColumn(ind);
			}
			double[] x = matrixData.NumericColumns[ind - matrixData.ExpressionColumnCount];
			float[] f = new float[x.Length];
			for (int i = 0; i < x.Length; i++){
				f[i] = (float) x[i];
			}
			return f;
		}

		private static string GetColumnName(IMatrixData matrixData, int ind){
			return ind < matrixData.ExpressionColumnCount
				? matrixData.ExpressionColumnNames[ind] : matrixData.NumericColumnNames[ind - matrixData.ExpressionColumnCount];
		}

		private static float[,] CalcExcludedPercentage(float[,] values){
			int n0 = values.GetLength(0);
			int n1 = values.GetLength(1);
			float[] v = new float[n0*n1];
			int[] ind0 = new int[n0*n1];
			int[] ind1 = new int[n0*n1];
			int count = 0;
			for (int i0 = 0; i0 < n0; i0++){
				for (int i1 = 0; i1 < n1; i1++){
					v[count] = values[i0, i1];
					ind0[count] = i0;
					ind1[count] = i1;
					count++;
				}
			}
			int[] o = ArrayUtils.Order(v);
			v = ArrayUtils.SubArray(v, o);
			ind0 = ArrayUtils.SubArray(ind0, o);
			ind1 = ArrayUtils.SubArray(ind1, o);
			double total = 0;
			foreach (float t in v){
				total += t;
			}
			float[,] result = new float[n0,n1];
			double sum = 0;
			for (int i = 0; i < v.Length; i++){
				result[ind0[i], ind1[i]] = (float) (sum/total);
				sum += v[i];
			}
			return result;
		}

		private static float[,] InvertRows(float[,] vals){
			float[,] result = new float[vals.GetLength(1),vals.GetLength(0)];
			int n = vals.GetLength(1);
			for (int i = 0; i < vals.GetLength(0); i++){
				for (int j = 0; j < n; j++){
					result[j, i] = vals[i, n - j - 1];
				}
			}
			return result;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] vals = ArrayUtils.Concat(mdata.ExpressionColumnNames, mdata.NumericColumnNames);
			int[] sel1 = vals.Length > 0 ? new[]{0} : new int[0];
			int[] sel2 = vals.Length > 1 ? new[]{1} : (vals.Length > 0 ? new[]{0} : new int[0]);
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Column 1", sel1){
						Values = vals, Repeats = true,
						Help =
							"Colums for the first dimension. Multiple choices can be made leading to the creation of multiple density maps."
					}
					,
					new MultiChoiceParam("Column 2", sel2){
						Values = vals, Repeats = true,
						Help = "Colums for the second dimension. The number has to be the same as for the first column."
					},
					new IntParam("Number of points", 300){
						Help =
							"This parameter defines the resolution of the density map. It specifies the number of pixels per dimension. Large " +
								"values may lead to increased computing times."
					}
				});
		}
	}
}