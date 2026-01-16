namespace Postica.BindingSystem
{

    class BindingSystemBridge
    {
        public static void Initialize()
        {
            // Bind Proxy part
            BindProxy.GetDefaultBindData = () => BindingSettings.Current.DefaultBindData;
        }
    }
}