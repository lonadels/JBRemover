namespace JBRemover
{
    internal class Project
    {
        public string? Name { get; init; }
        public string? Path { get; init; }

        public Project(string? name, string? path)
        {
            Name = name;
            Path = path;
        }

        public Project()
        {
        }
    }
}