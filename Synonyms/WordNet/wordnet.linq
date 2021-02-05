<Query Kind="Statements" />

var path = @"C:\Projects\WordNet\";

//-- osnovno izločanje sopomenk --//
using (var sw = new StreamWriter (path + "synonims.txt", false, Encoding.UTF8)) {

	{
		var files = new string [] { "verb.exc", "noun.exc", "adv.exc", "adj.exc" }.ToList ();
		files.ForEach (f => {
			var fName = Path.Combine (path + @"db\", f);
			using (var sr = new StreamReader (fName, Encoding.UTF8)) {
				while (!sr.EndOfStream) {
					var line = sr.ReadLine ();
					var parts = line.Split (' ').Select (w => w.Replace ("_", " ").Replace ("-", " ")).Distinct ().ToList ();
					if (parts.Count > 1) {
						sw.WriteLine (string.Join (",", parts.ToArray ()));
					}
				}
			}
		});
	}

	{
		var files = new string [] { "data.verb", "data.noun", "data.adv", "data.adj" }.ToList ();
		files.ForEach (f => {
			var fName = Path.Combine (path + @"db\", f);
			using (var sr = new StreamReader (fName, Encoding.UTF8)) {
				while (!sr.EndOfStream) {
					var line = sr.ReadLine ();
					if (string.IsNullOrWhiteSpace(line) || line [0] == ' ') { continue; }
					var parts = line.Split (' ');
					var words = new List<string> ();
					var i = 4;
					var word = parts [i];
					while ('a' <= word[0] && word[0] <= 'z') {
						words.Add (word.Replace ("_", " ").Replace ("-", " "));
						i+=2;
						word = parts [i].Replace ("_", " ").Replace ("-", " ");
					}
					words = words.Distinct ().ToList ();					
					if (words.Count > 1) {
						sw.WriteLine (string.Join (",", words.ToArray ()));
					}
				}
			}
		});
	}

}

//-- clean up duplikatov, oznak... --//

var finalList = new List<string> ();

using (var sr = new StreamReader (path + "synonims.txt", Encoding.UTF8)) {
	while (!sr.EndOfStream) {
		finalList.Add (sr.ReadLine ());
	}
}

finalList = finalList
	.Select (w => w
		.Replace ("a ", "")
		.Replace ("(a)", "")
		.Replace ("(p)", "")
		.Replace ("(ip)", "")
	)
	.Distinct ()
	.OrderBy (w => w)
	.ToList ();

using (var sw = new StreamWriter (path + "synonims.txt", false, Encoding.UTF8)) {
	finalList.ForEach (l => sw.WriteLine (l));
}