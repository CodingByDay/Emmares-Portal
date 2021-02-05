<Query Kind="Statements" />

var temHeader = new List<string> ();
var temBody = new List<string> ();
using (var sr = new StreamReader (@"E:\Temp\ELK\DirectAPI\CreateIndexTemplate.txt", Encoding.GetEncoding (1250))) {
	while (true) {
		var line = sr.ReadLine ();
		if (string.IsNullOrWhiteSpace (line)) { break; }
		temHeader.Add (line);
	}
	while (!sr.EndOfStream) {
		var line = sr.ReadLine ();
		temBody.Add (line);
	}
}

var comma = "";
var dict = new StringBuilder();
using (var sr = new StreamReader (@"E:\Temp\ELK\WordNet\synonims.txt", Encoding.UTF8)) {
	var breakAfter = 10;
	while (!sr.EndOfStream) {
		var line = sr.ReadLine ();
		var tokens = line.Split (',').ToList ();
		if (tokens.Count > 1) {
			var first = tokens [0];
			for (var i = 1; i < tokens.Count; i++) {
				dict.Append (comma);
				if (breakAfter-- < 0) {
					breakAfter = 10;
					dict.Append (Environment.NewLine);
				}
				comma = ", ";
				dict.Append ("\"" + first + "," + tokens [i] + "\"");
			}
		}
	}
}

var synonyms = dict.ToString ();
var body = "";
temBody.ForEach (l => {
	body += l.Replace ("{{synonyms}}", synonyms) + Environment.NewLine;
});

using (var sw = new StreamWriter (@"E:\Temp\ELK\DirectAPI\CreateIndexEMMARESCampaigns.txt", false, Encoding.UTF8)) {
	temHeader.ForEach (l => {
		sw.WriteLine (l.Replace ("{{len}}", body.Length.ToString ()));
	});
	sw.WriteLine ();
	sw.WriteLine (body);
}