namespace JPP.Core
{
    internal class Authentication
    {
        #region Public Variables

        private static Authentication _current;

        public static Authentication Current
        {
            get { return _current ?? (_current = new Authentication()); }
        }

        #endregion

        #region Private Variables

        private bool? _authenticated;

        #endregion

        public bool Authenticated()
        {
            if (_authenticated == null)
            {
                _authenticated = CheckLicense();
            }

            return (bool) _authenticated;
        }

        private bool CheckLicense()
        {
            #if DEBUG
            CoreMain.Log.Entry("Running in debug mode, no authentication required", Severity.Information);
            return true;
            #else
            //TODO: Implement authentication
            return true;
            #endif
        }
    }
}