namespace Deckbuilding.Windows
{
    public interface IWindow
    {
        public void Open();
        public void Close();
        void Init();
    }
}