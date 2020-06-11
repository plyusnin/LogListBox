namespace LogList.Control
{
    public class Child<TContent>
    {
        public Child(int Id, TContent Content)
        {
            this.Id      = Id;
            this.Content = Content;
        }

        public int      Id      { get; }
        public TContent Content { get; }
    }
}