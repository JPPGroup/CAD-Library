using Autodesk.AutoCAD.ApplicationServices;

namespace JPP.Core
{
    class Authentication
    {
        public static Authentication Current
        {
            get
            {
                if(_Current == null)
                {
                    _Current = new Authentication();
                }
                return _Current;
            }
        }

        private static Authentication _Current;

        private bool? _Authenticated;

        public bool Authenticated()
        {
            if(_Authenticated == null)
            {
                _Authenticated = CheckLicense();
            }

            return (bool)_Authenticated;
        }

        private bool CheckLicense()
        {
#if DEBUG
            Logger.Log("Running in debug mode, no authentication required", Severity.Information);
            return true;
#else

#endif
        }
    }
}
