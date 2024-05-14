using System.Text.RegularExpressions;

namespace Xavier.Tests.TowerOfHanoi;

// Define the necessary types
public class TowerOfHanoiState
{
    public List<int> A { get; set; }
    public List<int> B { get; set; }
    public List<int> C { get; set; }
}

public class TowerOfHanoiGoal
{
    public List<int> A { get; set; }
    public List<int> B { get; set; }
    public List<int> C { get; set; }
}

public class TowerOfHanoiAction
{
    public int Disk { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }

    public TowerOfHanoiAction()
    {
    }
    public TowerOfHanoiAction(int disk, string source, string target)
    {
        Disk = disk;
        Source = source;
        Target = target;
    }
    
    public TowerOfHanoiAction(string message)
    {
        // regex to parse the message
        var regex = new Regex(@"Move (\d+) from (\w) to (\w)");
        var match = regex.Match(message);

        if (match.Success)
        {
            Disk = int.Parse(match.Groups[1].Value);
            Source = match.Groups[2].Value;
            Target = match.Groups[3].Value;
        }
        else
        {
            throw new ArgumentException("Invalid action message format");
        }
    }

    public override string ToString()
    {
        return $"Move {Disk} from {Source} to {Target}";
    }

}