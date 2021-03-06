using System;
using System.Collections.Generic;
using System.Drawing;
using BasicLib.Param;
using BasicLib.Util;
using PerseusApi;

namespace PerseusPluginLib.Rearrange{
	public class ExpandMultiNumeric : IMatrixProcessing{
		public bool HasButton { get { return false; } }
		public Image ButtonImage { get { return null; } }
		public string HelpDescription { get { return ""; } }
		public string Name { get { return "Expand multi-numeric and string columns"; } }
		public string Heading { get { return "Matrix rearrangements"; } }
		public bool IsActive { get { return true; } }
		public float DisplayOrder { get { return 12; } }
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }
		public string HelpOutput { get { return ""; } }
		public HelpType HelpDescriptionType { get { return HelpType.PlainText; } }
		public HelpType HelpOutputType { get { return HelpType.PlainText; } }
		public HelpType[] HelpSupplTablesType { get { return new HelpType[0]; } }

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param1, ref IMatrixData[] supplTables, ProcessInfo processInfo){
			int[] multiNumCols = param1.GetMultiChoiceParam("Multi-numeric columns").Value;
			Array.Sort(multiNumCols);
			int[] stringCols = param1.GetMultiChoiceParam("String columns").Value;
			Array.Sort(stringCols);
			HashSet<int> multinumCols2 = new HashSet<int>(multiNumCols);
			HashSet<int> stringCols2 = new HashSet<int>(stringCols);
			if (multiNumCols.Length + stringCols.Length == 0){
				processInfo.ErrString = "Please select some columns.";
				return;
			}
			int rowCount = GetNewRowCount(mdata, multiNumCols, stringCols);
			float[,] expVals = new float[rowCount,mdata.ExpressionColumnCount];
			List<string[]> stringC = new List<string[]>();
			for (int i = 0; i < mdata.StringColumnCount; i++){
				stringC.Add(new string[rowCount]);
			}
			List<double[]> numC = new List<double[]>();
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				numC.Add(new double[rowCount]);
			}
			List<string[][]> catC = new List<string[][]>();
			for (int i = 0; i < mdata.CategoryColumnCount; i++){
				catC.Add(new string[rowCount][]);
			}
			List<double[][]> multiNumC = new List<double[][]>();
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++){
				multiNumC.Add(new double[rowCount][]);
			}
			int count = 0;
			for (int i = 0; i < mdata.RowCount; i++){
				string err;
				int entryCount = GetEntryCount(i, mdata, multiNumCols, stringCols, out err);
				if (err != null){
					processInfo.ErrString = err;
					return;
				}
				bool empty = entryCount == 0;
				entryCount = Math.Max(entryCount, 1);
				for (int j = 0; j < entryCount; j++){
					for (int k = 0; k < mdata.ExpressionColumnCount; k++){
						expVals[count + j, k] = mdata[i, k];
					}
					for (int k = 0; k < mdata.NumericColumnCount; k++){
						numC[k][count + j] = mdata.NumericColumns[k][i];
					}
					for (int k = 0; k < mdata.CategoryColumnCount; k++){
						catC[k][count + j] = mdata.CategoryColumns[k][i];
					}
				}
				for (int k = 0; k < mdata.MultiNumericColumnCount; k++){
					if (multinumCols2.Contains(k)){
						if (empty){
							multiNumC[k][count] = new double[0];
						} else{
							double[] vals = mdata.MultiNumericColumns[k][i];
							for (int j = 0; j < entryCount; j++){
								multiNumC[k][count + j] = new[]{vals[j]};
							}
						}
					} else{
						for (int j = 0; j < entryCount; j++){
							multiNumC[k][count + j] = mdata.MultiNumericColumns[k][i];
						}
					}
				}
				for (int k = 0; k < mdata.StringColumnCount; k++){
					if (stringCols2.Contains(k)){
						if (empty){
							stringC[k][count] = "";
						} else{
							string[] vals = mdata.StringColumns[k][i].Split(';');
							for (int j = 0; j < entryCount; j++){
								stringC[k][count + j] = vals[j];
							}
						}
					} else{
						for (int j = 0; j < entryCount; j++){
							stringC[k][count + j] = mdata.StringColumns[k][i];
						}
					}
				}
				count += entryCount;
			}
			int[] multiNumComplement = ArrayUtils.Complement(multiNumCols, mdata.MultiNumericColumnCount);
			List<double[][]> toBeTransformed = ArrayUtils.SubList(multiNumC, multiNumCols);
			multiNumC = ArrayUtils.SubList(multiNumC, multiNumComplement);
			foreach (double[][] d in toBeTransformed){
				numC.Add(Transform(d));
			}
			mdata.SetData(mdata.Name, mdata.ExpressionColumnNames, expVals, mdata.StringColumnNames, stringC,
				mdata.CategoryColumnNames, catC,
				new List<string>(ArrayUtils.Concat(mdata.NumericColumnNames,
					ArrayUtils.SubList(mdata.MultiNumericColumnNames, multiNumCols))), numC,
				new List<string>(ArrayUtils.SubArray(mdata.MultiNumericColumnNames, multiNumComplement)), multiNumC);
		}

		private static double[] Transform(IList<double[]> doubles){
			double[] result = new double[doubles.Count];
			for (int i = 0; i < result.Length; i++){
				result[i] = doubles[i][0];
			}
			return result;
		}

		private int GetEntryCount(int row, IMatrixData mdata, IList<int> multiNumCols, IList<int> stringCols, out string err){
			int[] multiNumEntryCount = new int[multiNumCols.Count];
			for (int i = 0; i < multiNumEntryCount.Length; i++){
				multiNumEntryCount[i] = GetEntryCount(mdata.MultiNumericColumns[multiNumCols[i]][row]);
			}
			int[] stringEntryCount = new int[stringCols.Count];
			for (int i = 0; i < stringEntryCount.Length; i++){
				stringEntryCount[i] = GetEntryCount(mdata.StringColumns[stringCols[i]][row]);
			}
			int[] v = ArrayUtils.UniqueValues(ArrayUtils.Concat(multiNumEntryCount, stringEntryCount));
			if (v.Length > 1){
				err = "Inconsistent number of values in row " + row + ".";
				return -1;
			}
			err = null;
			return v[0];
		}

		private int GetEntryCount(string s){
			string[] q = s.Length > 0 ? s.Split(';') : new string[0];
			return q.Length;
		}

		private static int GetEntryCount(ICollection<double> x){
			return x.Count;
		}

		private static int GetNewRowCount(IMatrixData mdata, IList<int> multiNumCols, IList<int> stringCols){
			return multiNumCols.Count > 0
				? GetNewRowCount(mdata.MultiNumericColumns[multiNumCols[0]]) : GetNewRowCount(mdata.StringColumns[stringCols[0]]);
		}

		private static int GetNewRowCount(IEnumerable<string> stringColumn){
			int count = 0;
			foreach (string s in stringColumn){
				if (s.Length > 0){
					count += s.Split(';').Length;
				} else{
					count++;
				}
			}
			return count;
		}

		private static int GetNewRowCount(IEnumerable<double[]> multiNumericColumn){
			int count = 0;
			foreach (double[] s in multiNumericColumn){
				if (s.Length > 0){
					count += s.Length;
				} else{
					count++;
				}
			}
			return count;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Multi-numeric columns"){
						Values = mdata.MultiNumericColumnNames, Value = new int[0],
						Help = "Select here the multi-numeric colums that should be expanded."
					},
					new MultiChoiceParam("String columns"){
						Values = mdata.StringColumnNames, Value = new int[0],
						Help = "Select here the string colums that should be expanded."
					}
				});
		}
	}
}