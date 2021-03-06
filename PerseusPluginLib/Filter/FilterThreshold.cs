using System;
using System.Collections.Generic;
using System.Drawing;
using BasicLib.Param;
using BasicLib.Util;
using PerseusApi;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Filter{
	internal enum FilteringMode{
		Minimum,
		Maximum,
		Between,
		Outside
	}

	public class FilterThreshold : IMatrixProcessing{
		public bool HasButton { get { return false; } }
		public Image ButtonImage { get { return null; } }
		public string Name { get { return "Filter threshold"; } }
		public string Heading { get { return "Filter"; } }
		public bool IsActive { get { return true; } }
		public float DisplayOrder { get { return -3; } }
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }
		public HelpType HelpDescriptionType { get { return HelpType.PlainText; } }
		public HelpType HelpOutputType { get { return HelpType.PlainText; } }
		public HelpType[] HelpSupplTablesType { get { return new HelpType[0]; } }
		public string HelpDescription {
			get{
				return
					"Rows/columns of the expression matrix are filtered to contain at least the specified numbers of entries that are " +
						"greater than or equal to the specified value.";
			}
		}
		public string HelpOutput { get{
			return
				"The matrix of expression values is constrained to contain only these rows/columns that fulfil the requirement.";
		} }

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ProcessInfo processInfo){
			bool rows = param.GetSingleChoiceParam("Matrix access").Value == 0;
			int minValids = param.GetIntParam("Min. number of values").Value;
			SingleChoiceWithSubParams modeParam = param.GetSingleChoiceWithSubParams("Mode");
			int modeInd = modeParam.Value;
			if (modeInd != 0 && mdata.CategoryRowNames.Count == 0){
				processInfo.ErrString = "No grouping is defined.";
				return;
			}
			if (modeInd != 0 && !rows){
				processInfo.ErrString = "Group-wise filtering can only be appled to rows.";
				return;
			}
			SingleChoiceWithSubParams x = param.GetSingleChoiceWithSubParams("Values should be");
			Parameters subParams = x.GetSubParameters();
			int shouldBeIndex = x.Value;
			FilteringMode filterMode;
			double threshold;
			double threshold2 = double.NaN;
			switch (shouldBeIndex){
				case 0:
					filterMode = FilteringMode.Minimum;
					threshold = subParams.GetDoubleParam("Minimum").Value;
					break;
				case 1:
					filterMode = FilteringMode.Maximum;
					threshold = subParams.GetDoubleParam("Maximum").Value;
					break;
				case 2:
					filterMode = FilteringMode.Between;
					threshold = subParams.GetDoubleParam("Minimum").Value;
					threshold2 = subParams.GetDoubleParam("Maximum").Value;
					break;
				case 3:
					filterMode = FilteringMode.Outside;
					threshold = subParams.GetDoubleParam("Minimum").Value;
					threshold2 = subParams.GetDoubleParam("Maximum").Value;
					break;
				default:
					throw new Exception("Should not happen.");
			}
			if (modeInd != 0){
				int gind = modeParam.GetSubParameters().GetSingleChoiceParam("Grouping").Value;
				string[][] groupCol = mdata.CategoryRows[gind];
				NonzeroFilterGroup(minValids, mdata, param, modeInd == 2, threshold, threshold2, filterMode, groupCol);
			} else{
				NonzeroFilter1(rows, minValids, mdata, param, threshold, threshold2, filterMode);
			}
		}

		private static void NonzeroFilterGroup(int minValids, IMatrixData mdata, Parameters param, bool oneGroup,
			double threshold, double threshold2, FilteringMode filterMode, IList<string[]> groupCol){
			List<int> valids = new List<int>();
			string[] groupVals = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
			Array.Sort(groupVals);
			int[][] groupInds = FilterValidValues.CalcGroupInds(groupVals, groupCol);
			for (int i = 0; i < mdata.RowCount; i++){
				int[] counts = new int[groupVals.Length];
				for (int j = 0; j < mdata.ExpressionColumnCount; j++){
					if (Valid(mdata[i, j], threshold, threshold2, filterMode)){
						for (int k = 0; k < groupInds[j].Length; k++){
							if (groupInds[j][k] >= 0){
								counts[groupInds[j][k]]++;
							}
						}
					}
				}
				if ((oneGroup ? ArrayUtils.Max(counts) : ArrayUtils.Min(counts)) >= minValids){
					valids.Add(i);
				}
			}
			PerseusPluginUtils.FilterRows(mdata, param, valids.ToArray());
		}

		private static void NonzeroFilter1(bool rows, int minValids, IMatrixData mdata, Parameters param, double threshold,
			double threshold2, FilteringMode filterMode){
			if (rows){
				List<int> valids = new List<int>();
				for (int i = 0; i < mdata.RowCount; i++){
					int count = 0;
					for (int j = 0; j < mdata.ExpressionColumnCount; j++){
						if (Valid(mdata[i, j], threshold, threshold2, filterMode)){
							count++;
						}
					}
					if (count >= minValids){
						valids.Add(i);
					}
				}
				PerseusPluginUtils.FilterRows(mdata, param, valids.ToArray());
			} else{
				List<int> valids = new List<int>();
				for (int j = 0; j < mdata.ExpressionColumnCount; j++){
					int count = 0;
					for (int i = 0; i < mdata.RowCount; i++){
						if (Valid(mdata[i, j], threshold, threshold2, filterMode)){
							count++;
						}
					}
					if (count >= minValids){
						valids.Add(j);
					}
				}
				PerseusPluginUtils.FilterColumns(mdata, param, valids.ToArray());
			}
		}

		private static bool Valid(double data, double threshold, double threshold2, FilteringMode filterMode){
			switch (filterMode){
				case FilteringMode.Minimum:
					return data >= threshold;
				case FilteringMode.Maximum:
					return data <= threshold;
				case FilteringMode.Between:
					return data >= threshold && data <= threshold2;
				case FilteringMode.Outside:
					return data < threshold || data > threshold2;
			}
			throw new Exception("Never get here.");
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			Parameters[] subParams = new Parameters[3];
			subParams[0] = new Parameters(new Parameter[0]);
			subParams[1] = new Parameters(new Parameter[]{new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames}});
			subParams[2] = new Parameters(new Parameter[]{new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames}});
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Matrix access"){
						Values = new[]{"Rows", "Columns"},
						Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
					},
					new DoubleParam("Threshold", 0){Help = "Value defining which entry is counted as a valid value."},
					new IntParam("Min. number of values", 3){
						Help =
							"If a row/column has less than the specified number of values larger than the threshold it will be discarded in the output."
					},
					new SingleChoiceWithSubParams("Mode")
					{Values = new[]{"In total", "In each group", "In at least one group"}, SubParams = subParams},
					new SingleChoiceWithSubParams("Values should be"){
						Values = new[]{"Greater than", "Less than", "Between", "Outside"},
						SubParams =
							new[]{
								new Parameters(new Parameter[]
								{new DoubleParam("Minimum", 0){Help = "Value defining which entry is counted as a valid value."}}),
								new Parameters(new Parameter[]
								{new DoubleParam("Maximum", 0){Help = "Value defining which entry is counted as a valid value."}}),
								new Parameters(new Parameter[]{
									new DoubleParam("Minimum", 0){Help = "Value defining which entry is counted as a valid value."},
									new DoubleParam("Maximum", 0){Help = "Value defining which entry is counted as a valid value."}
								}),
								new Parameters(new Parameter[]{
									new DoubleParam("Minimum", 0){Help = "Value defining which entry is counted as a valid value."},
									new DoubleParam("Maximum", 0){Help = "Value defining which entry is counted as a valid value."}
								})
							}
					},
					PerseusPluginUtils.GetFilterModeParam()
				});
		}
	}
}