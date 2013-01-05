using System;

namespace PerseusApi{
	public class Settings{
		public int Nthreads { get; set; }
		public string TempFolder { get; set; }
		public string[] CommentPrefix { get; set; }

		public Settings(){
			Nthreads = Environment.ProcessorCount;
			CommentPrefix = new[]{"#", "!"};
		}
	}
}