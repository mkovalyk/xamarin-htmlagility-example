using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Views.InputMethods;
using Java.Lang;
using Boolean = System.Boolean;
using Exception = System.Exception;
using Math = System.Math;
using String = System.String;
using Android.Content.PM;
using Android.Net;
using System.Net;

namespace AmazonPriceChecker_mono
{
    using System.Globalization;
    using AmazonPriceChecker_mono;
    using Android.Util;
    using Android.Graphics;
    using Android.Text;
    using Android.Speech.Tts;
    using Java.Util;
    using System.Collections.Generic;

    using Uri = Android.Net.Uri;

    [Activity(Label = "AmazonPriceChecker", MainLauncher = true, Icon = "@drawable/Icon",
        ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : Activity
    {
        // new service urls
        private ImageButton _btnCapture;
        private ImageView _btnRefresh;
        //private SampleView btnRefresh;
        //private SampleImageButton btnRefresh;
        private ImageButton _btnGo;
        private static EditText _txtBarCode;
        private ImageView _imageView;
        private TextView _txtTitle;
        private TextView _txtAmazonPrice;
        //private TextView _txtCategory;
        private TextView _txtWeight;
        private TextView _txtSalesRank;
        private string _amazonPrice = string.Empty;
        private string _category = string.Empty;
        private string _weight = string.Empty;
        private string _salesRank = string.Empty;
        private Button _btnBuy;
        private TableRow _trTitles;
        private TableRow _trTotalRow;
        private TextView _txtUsedTotal;
        private TextView _txtNewTotal;
        private TextView _txtFBATotal;
        private TextView _txtProfitHeader;
        private TextView _txtProfitTotal;
        private TextView _txtTitleRight;
        private TextView _txtCounter;
        private TextView _txtBuy;
        private Button _btnNum;
        private ImageButton _btnMulti;
        private ImageView _btnCamel;
        private ImageView _btnLink;
        private ImageButton _btnOperationMode;
        private View _tl;
        private static readonly View[] Mykeys = new View[12];
        private Button _btnSearchText;
        private Button _btnCancelText;
        private static String _barcode = string.Empty;
        private GestureDetector _gestureDetector;
        // for double tap event
        private MyOnClickOnTouchListener _onClickTouchListener;
        private ItemReturnInfo _itemReturnInfo;

        private string _lastBarcode = "";
        private static IEngine _engine;
        private ISoundEngine _soundEngine;
        private TextToSpeech _mts;

        private const int RequestCodeForScanning = 1;
        public const int RequestCodeForCriteria = 2;
        public const int RequestCodeForConfig = 4;
        private const int RequestCodeForDisplayingMultipleItems = 6;
        public const int RequestCodeForDownloading = 8;
        private const int RequestCodeForMenu = 9;
        private const int RequestCodeForDownloadingUpdate = 10;
        public const int RequestCodeForBasicTrigger = 11;
        public const int RequestCodeForAdvancedTrigger = 12;

        private Intent _scanIntent;
        private ItemReturnInfo[] _itemreturninfos;
        //
        // This event is written to capture the back key pressed
        //
        public static IEngine Engine
        {
            get { return _engine ?? (_engine = new MockEngine()); }
        }

        public override void OnBackPressed()
        {
        }

        public MainActivity()
        {
            if (_engine != null) return;

            try
            {
                _engine = new MockEngine();
                _engine.EvtPerformLive += RespondToLive;
                _engine.EvtFinishLookUp += FinishLookup;
                _engine.EvtCheckVersion += HandleEvtCheckVersion;
            }
            catch (Exception ex)
            {
                StaticHolder.ReportCrash("MainActivity", ex);
            }
        }

        private void RespondToLive(ItemReturnInfo[] itemreturninfos)
        {
            RunOnUiThread(StartRefreshAnimation);
        }

        private void FinishLookup(ItemReturnInfo[] itemreturninfos)
        {
            _itemreturninfos = itemreturninfos;
            RunOnUiThread(FinishLookupUI);
        }

        private void FinishLookupUI()
        {
            StopRefreshAnimation();

            if ((_itemreturninfos == null) || (_itemreturninfos.Length == 0))
            {
                _itemreturninfos = new ItemReturnInfo[1];
                _itemreturninfos[0] = new ItemReturnInfo
                    {
                        InputBarcode = StaticHolder.UpcInput,
                        Result = LookupReturnValue.ProgramError,
                        OutputText = "Program Error"
                    };

                StaticHolder.ReportCrash(string.Format("FinishLookupUI {0}", _itemreturninfos == null),
                                         new ApplicationException("no result"));
            }

            string currentBarcode = StaticHolder.UpcInput;
            if (_itemreturninfos[0].InputBarcode != currentBarcode)
            {
                //user already scan a new barcode, so we ignore this old one
                //uncomment the code below in release
#if !DEBUG
			//	return;
#endif
            }

            StaticHolder._returnInfos = _itemreturninfos;

            if (StaticHolder._returnInfos.Length > 1)
            {
                //multiple results
                if (!PreferenceClass.DisableSound)
                {
                    _soundEngine.PlaySound(LookupReturnValue.Multiple);
                }

                try
                {
                //multiple results, showing list box
                ShowMultipleItems();
            }
                catch (Exception ex)
                {
                    StaticHolder.ReportCrash("FinishLookup.ShowMultipleItems", ex);
                }
            }
            else
            {
                try
                {
                    //single result, display result
                    DoDisplayResult(StaticHolder._returnInfos[0]);
                }
                catch (Exception ex)
                {
                    StaticHolder.ReportCrash("FinishLookup.DoDisplayResult", ex);
                }
            }
        }

        private void DoAudioNotification()
        {
            if (PreferenceClass.DisableSound)
            {
                //disable sound
                return;
            }

            if ((StaticHolder._returnInfos[0].Result == LookupReturnValue.ProgramError) &&
                (!string.IsNullOrEmpty(StaticHolder._returnInfos[0].OutputText)))
            {
                _mts.Speak(StaticHolder._returnInfos[0].OutputText, QueueMode.Flush,
                           new Dictionary<string, string>());
            }
            else if (!PreferenceClass.VoiceOnly)
            {
                _soundEngine.PlaySound(StaticHolder._returnInfos[0].Result);
            }
        }

        private void InitTTS()
        {
            Android.Util.Log.Debug("000000---", "Before InitTTS");

            _mts = new TextToSpeech(this, _onClickTouchListener);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            try
            {
                PreferenceClass.ApplicationContext = this;
//                _scanIntent = new Intent(this, Class.ForName("com.ebay.rlsample.RLSampleScannerActivity"));
//                _scanIntent.PutExtra("intent_multi_scan", false);


                RequestWindowFeature(WindowFeatures.CustomTitle);
                SetContentView(Resource.Layout.Main);
                Window.SetFeatureInt(WindowFeatures.CustomTitle, Resource.Layout.i_title);

                String appVer = PackageManager.GetPackageInfo(PackageName, 0).VersionName;
                _txtTitleRight = (TextView) FindViewById(Resource.Id.title_right);
                _txtTitleRight.Text = appVer;

                if (!PreferenceClass.DoesPreferenceExists())
                {
                    _engine.GetDefaultPreference();
                }

                if ((string.IsNullOrEmpty(PreferenceClass.UserID)) || (string.IsNullOrEmpty(PreferenceClass.Password)))
                {
                    //if user id is null or empty, start the account activity
                    Intent it = new Intent(ApplicationContext, typeof (ConfigActivity));
                    it.PutExtra(StaticHolder.LastUpdate, _engine.LastApplicationUpdate);
                    StartActivity(it);
                }

                //gestureDetector = new GestureDetector(context, new MyGestureActivity());

                _btnCapture = (ImageButton) FindViewById(Resource.Id.btnCapture);
                _btnRefresh = (ImageView) FindViewById(Resource.Id.btnRefresh);
                _btnGo = (ImageButton) FindViewById(Resource.Id.btnGo);
                _btnGo.Visibility = ViewStates.Gone;
                _txtBarCode = (EditText) FindViewById(Resource.Id.btnBarCode);
                _txtBarCode.InputType = 0;
                _txtCounter = (TextView) FindViewById(Resource.Id.txtCounter);
                _txtProfitHeader = (TextView) FindViewById(Resource.Id.tbHeaderProfit);
                _txtProfitTotal = (TextView) FindViewById(Resource.Id.txtProfitTotal);

                _imageView = (ImageView) FindViewById(Resource.Id.imageview);
                _txtTitle = (TextView) FindViewById(Resource.Id.txtTitle);
                //_txtCategory = (TextView) FindViewById(Resource.Id.txtCategory);
                _txtAmazonPrice = (TextView) FindViewById(Resource.Id.txtAmazonPrice);
                _txtWeight = (TextView) FindViewById(Resource.Id.txtWeight);
                _txtSalesRank = (TextView) FindViewById(Resource.Id.txtSalesRank);
                _txtBuy = (TextView) FindViewById(Resource.Id.txtBuy);
                _trTotalRow = (TableRow) FindViewById(Resource.Id.totalRow);
                _trTitles = (TableRow) FindViewById(Resource.Id.titles);
                _btnBuy = (Button) FindViewById(Resource.Id._btnBuy);
                _txtUsedTotal = (TextView) FindViewById(Resource.Id.txtUsedTotal);
                _txtNewTotal = (TextView) FindViewById(Resource.Id.txtNewTotal);
                _txtFBATotal = (TextView) FindViewById(Resource.Id.txtFBATotal);
                _btnMulti = (ImageButton) FindViewById(Resource.Id.btnMulti);
                _btnCamel = (ImageView) FindViewById(Resource.Id.btnCamel);
                _btnLink = (ImageView) FindViewById(Resource.Id.btnLink);
                _btnOperationMode = (ImageButton) FindViewById(Resource.Id.btnOperationMode);
                _btnNum = (Button) FindViewById(Resource.Id.btnNum);
                _onClickTouchListener = new MyOnClickOnTouchListener(this);
                _table = (LinearLayout) FindViewById(Resource.Id.linLayoutTable);
                _tableLayout = (TableLayout) FindViewById(Resource.Id.dynamic_table);
                //scrollViewTableRow = (LinearLayout)FindViewById (Resource.Id.scrollViewTableRow);
                _dynamicTable = (TableLayout) FindViewById(Resource.Id.dynamic_table);

                _btnCapture.Click += StartScanning;
                //_btnCapture.SetOnClickListener(_onClickTouchListener);
                _btnRefresh.SetOnClickListener(_onClickTouchListener);
                _btnGo.SetOnClickListener(_onClickTouchListener);
                _btnBuy.SetOnClickListener(_onClickTouchListener);
                _btnCamel.SetOnClickListener(_onClickTouchListener);
                _btnLink.SetOnClickListener(_onClickTouchListener);
                _btnMulti.SetOnClickListener(_onClickTouchListener);
                //_btnOperationMode.SetOnClickListener (_onClickTouchListener);
                _imageView.SetOnClickListener(_onClickTouchListener);
                _btnNum.SetOnClickListener(_onClickTouchListener);
                _txtBarCode.AddTextChangedListener(_onClickTouchListener);
                _txtBuy.SetOnClickListener(_onClickTouchListener);
                _gestureDetector = new GestureDetector(this, _onClickTouchListener);
                _gestureDetector.SetOnDoubleTapListener(_onClickTouchListener);
                //LinearLayout lm = Resource.Id.linLayoutMain;


                _txtBarCode.SetOnKeyListener(_onClickTouchListener);
                //looks like the enter is captured in onkeyup, so we comment out this one
                _txtBarCode.SetOnTouchListener(_onClickTouchListener);

                //Create a new instance of our Scanner
                //_scanner = new MobileBarcodeScanner(this);

                // set the Operation Type
                string tempString = _engine.GetOperationModeName(PreferenceClass.OperationMode).ToUpper().Trim();
                _btnOperationMode.SetBackgroundDrawable(Resources.GetDrawable(GetResourceFromString(tempString)));

                //for demo user, initilize remaining 
                _txtCounter.Visibility = PreferenceClass.IsDemoUser ? ViewStates.Visible : ViewStates.Invisible;
                _txtCounter.Text = PreferenceClass.CheckCount.ToString(CultureInfo.CurrentCulture);


                FindViews();
                SetListeners();
                InitTTS();

                Intent itt = Intent;
                if (null != itt)
                {
                    if (itt.Action != null)
                    {
                        if (itt.Action.Equals("android.intent.action.PRODUCT"))
                        {
                            Bundle b = itt.Extras;
                            _txtBarCode.Text = b.GetString("ProductId");
                        }
                    }
                }
                _scrollViewTableRow = (LinearLayout) FindViewById(Resource.Id.scrollViewTableRow);
                _scrollViewTableRow.Clickable = true;
                _scrollViewTableRow.SetOnTouchListener(_onClickTouchListener);


                string deviceKey;

                try
                {
                    deviceKey = StaticHolder.GetDeviceKey_Combined(this);
                }
                catch (Exception)
                {
#if DEBUG
                    deviceKey = "testdevice";
#else
                    throw;
#endif
                }
                _soundEngine = new SoundEngine(this);
                _engine.Init(deviceKey, _soundEngine);

                _txtBarCode.RequestFocus();
                _txtBarCode.SelectAll();

                _engine.LoadAdvTriggers();
            }
            catch (Exception ex)
            {
                if (_txtTitle != null)
                {
                    _txtTitle.SetTextSize(ComplexUnitType.Sp, 11);

#if DEBUG
                    _txtTitle.Text = ex.Message + "\r\n" + ex.StackTrace;
#else
				_txtTitle.Text = ex.Message;
#endif
                }

                StaticHolder.ReportCrash("OnCreate", ex);
            }

            //No SD card

            if (string.IsNullOrEmpty(StaticHolder.ApplicationPath))
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Built-in Storage is not available");
                builder.SetMessage("Disconnect phone from computer or turn off USB mass storage and restart app");
            //    builder.SetPositiveButton("OK", (s, args) => Finish());

                builder.SetNegativeButton("OK", (s, args) => ((Dialog) s).Dismiss());


                var dialog = builder.Create();
                dialog.Show();
            }

            //todo test add intro later
            // DisplayIntro ();
        }

        protected override void OnStart()
        {
            base.OnStart();

            // display operation mode
            SetModeDrawable();

            // check software version
            string time = PreferenceClass.LastUpdateTime;
            DateTime today = DateTime.Today;
            if (string.IsNullOrEmpty(time))
            {
                // first checking the software version
                CheckForUpdates();
            }
            else
            {
                // not the first time checking the software version, so we only check once every 7 days.
                DateTime checkDate;
                DateTime.TryParse(time, out checkDate);

                if (today.CompareTo(checkDate.AddDays(7.0)) <= 0)
                {
                    return; // no need to check if last check is within 7 days 
            }

                CheckForUpdates();
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            // release resource, so if count of columns or rows was changed - recreate the array.
            //	_soundEngine = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _mts.Shutdown();
            _mts = null;
            _engine = null;
        }

        private void CheckForUpdates()
        {
            try
            {
            string lastCheckedVersion = PreferenceClass.LastCheckedVersion;

            if (string.IsNullOrEmpty(lastCheckedVersion))
            {
                    // first time check
                PackageInfo pInfo = PackageManager.GetPackageInfo(PackageName, 0);
                lastCheckedVersion = pInfo.VersionName;
                PreferenceClass.LastCheckedVersion = lastCheckedVersion;
            }

                if (string.IsNullOrEmpty(lastCheckedVersion)) return;

                ConnectivityManager cm = (ConnectivityManager) GetSystemService(
                    ConnectivityService);
                NetworkInfo info = cm.ActiveNetworkInfo;

                if (info == null) return; //no Internet, no need to check

                //save check date
                    PreferenceClass.LastUpdateTime = DateTime.Today.ToString(CultureInfo.InvariantCulture);

                //check if we have update version
                    _engine.CheckForUpdate(lastCheckedVersion);
                }
            catch (Exception ex)
            {
                StaticHolder.ReportCrash("CheckForUpdates", ex);
            }
        }

        //		protected override void OnStart ()
        private void HandleEvtCheckVersion(string version, string message)
        {
            // save last checked version to Preference class;
            PreferenceClass.LastCheckedVersion = version;
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(version + "  " + Resources.GetString(Resource.String.update_available_title));
            builder.SetMessage(message);
            builder.SetPositiveButton(Resource.String.button_ok, OKClicked); //OK to update
            builder.SetNegativeButton(Resource.String.cancel, (s, args) => ((Dialog) s).Dismiss());
            var dialog = builder.Create();
            dialog.Show();
        }

        private void SetModeDrawable()
        {
            // set   image  appropriate to current mode
            string mode = PreferenceClass.OperationMode.ToString();
            //				OperationModeType type;
            if (!String.IsNullOrEmpty(mode))
            {
                _btnOperationMode.SetBackgroundDrawable(Resources.GetDrawable(GetResourceFromString(mode)));
            }
        }


        private void OKClicked(object sender, DialogClickEventArgs e)
        {
            Intent intent = new Intent(Intent.ActionView, Uri.Parse(StaticHolder.AppStoreURL));
            StartActivityForResult(intent, RequestCodeForDownloadingUpdate);
        }

        private void StartScanning(object sender, EventArgs e)
        {
            HideKeyBoard();
            _tl.Visibility = ViewStates.Invisible;
            LaunchRedLaser();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            _gestureDetector.OnTouchEvent(e);
            return false;
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            // if (hasFocus)
            //{
            //((AnimationDrawable)_btnRefresh.Drawable).Start();
            //}
        }

        private int GetResourceFromString(string name)
        {
            switch (name)
            {
                case "DB":
                    return Resource.Drawable.DB;
                case "NF":
                    return Resource.Drawable.NF;
                case "MR":
                    return Resource.Drawable.MR;
                case "LV":
                    return Resource.Drawable.LV;
                default:
                    return Resource.Drawable.DB;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            try
            {
                if (resultCode == Result.Ok)
                {
                    switch (requestCode)
                    {
                        case RequestCodeForScanning:

//                            string barcode = data.GetStringExtra("com.ebay.redlasersdk.barcode");
//                            string txt = !string.IsNullOrEmpty(barcode) ? barcode : "";

							string txt = data.GetStringExtra("barcode");
                            StartSearch(txt);
                            break;

                        case 5:
                            _mts = new TextToSpeech(this, _onClickTouchListener);
                            _mts.SetLanguage(Locale.Us);
                            break;

                        case RequestCodeForDisplayingMultipleItems:
                            // One of the items of Multiple Results selected   
                            _btnMulti.Visibility = ViewStates.Visible;
                            _btnCamel.Visibility = ViewStates.Visible;
                            _btnLink.Visibility = ViewStates.Visible;
                            int index = data.GetIntExtra("SELECTED_MEDIA_INDEX", 0);
                            DoDisplayResult(StaticHolder._returnInfos[index]);
                            break;

                        case RequestCodeForMenu:
                            bool shouldExit = data.GetBooleanExtra("shouldExit", false);
                            if (shouldExit)
                                Finish();
                            bool dataTest = data.GetBooleanExtra("dataTest", false);
                            bool closeDownload = data.GetBooleanExtra("closeDownload", false);
                            if (dataTest || closeDownload)
                            {
                                _engine.RefreshCounter();
                                _txtBarCode.Text = "0977240606";
                                CheckPrice(OperationModeType.DB);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _txtTitle.SetTextSize(ComplexUnitType.Sp, 11);
                _txtTitle.Text = ex.Message;

                StaticHolder.ReportCrash("OnActivityResult", ex);
            }
        }

        private class MyOnClickOnTouchListener : Java.Lang.Object, View.IOnClickListener, View.IOnTouchListener,
                                                 View.IOnKeyListener, ITextWatcher, TextToSpeech.IOnInitListener,
                                                 GestureDetector.IOnDoubleTapListener,
                                                 GestureDetector.IOnGestureListener
        {
            private readonly MainActivity _ma;

            public MyOnClickOnTouchListener(MainActivity ma)
            {
                _ma = ma;
            }

            public void OnInit(OperationResult status)
            {
                Android.Util.Log.Debug("OnTTSInit", status == OperationResult.Success ? "Init completed" : "Init failed");

                if (_ma._mts == null) return;


                    LanguageAvailableResult result = _ma._mts.IsLanguageAvailable(Locale.Us);
                    if (result == LanguageAvailableResult.MissingData || result == LanguageAvailableResult.NotSupported)
                    {
                        Android.Util.Log.Error("000000---", "Language is not supported");
                        Intent installIntent = new Intent();
                        installIntent.SetAction(TextToSpeech.Engine.ActionInstallTtsData);
                        _ma.StartActivity(installIntent);
                    }
                    else
                    {
                        _ma._mts.SetLanguage(Locale.Us);
                    }
                }

            void ITextWatcher.AfterTextChanged(IEditable s)
            {
                if ((s.Length() != 10) && (s.Length() != 13)) return;

                if ((!PreferenceClass.AutoSubmitISBN) || (!_engine.VerifyValidCode(s.ToString()))) return;

                        _ma.ToggleNumPadVisibility();

                        StaticHolder.DisPlay8Rows = PreferenceClass.LargerPricingRow;
                        _ma.CheckPrice(PreferenceClass.OperationMode);
                        _txtBarCode.SelectAll();
                    }

            public void BeforeTextChanged(ICharSequence s, int start, int count, int after)
            {
                //require to implement for interface
            }

            public void OnTextChanged(ICharSequence s, int start, int before, int count)
            {
                //require to implement for interface
            }

            public bool OnTouch(View v, MotionEvent e)
            {
                switch (v.Id)
                {
                    case Resource.Id.numkeypad_go:
                        break;
                    case Resource.Id.btnBarCode:
                        switch (e.Action)
                        {
                            case MotionEventActions.Down:
                                //ma.hideKeyBoard();
                                _txtBarCode.Text = string.Empty;
                                _ma.ToggleNumPadVisibility();
                                _ma.HideKeyBoard();
                                return true;
                            case MotionEventActions.Up:
                                break;
                        }
                        break;
                    case Resource.Id.scrollViewTableRow:
                    case Resource.Id.dynamic_table:
                        _ma._gestureDetector.OnTouchEvent(e);
                        return false;
                }
                return false;
            }

            public void OnClick(View v)
            {
                try
                {
                switch (v.Id)
                {
                    case Resource.Id.numkeypad_go:
                        _ma.ToggleNumPadVisibility();
                        try
                        {
                            StaticHolder.DisPlay8Rows = PreferenceClass.LargerPricingRow;
                            _ma.CheckPrice(PreferenceClass.OperationMode);

                                //todo test this is to simulate user hundreds of input
#if DEBUG
                                //  _ma.StartInputWorker(10, 2000); //number of inputs, interval time
#endif
                        }
                        catch (Exception ex)
                        {
                                StaticHolder.ReportCrash("OnClick", ex);
                        }

                        _txtBarCode.SelectAll();
                        break;

                    case Resource.Id.btnRefresh:
                        if (!string.IsNullOrEmpty(_txtBarCode.Text.Trim()))
                        {
                            _ma._lastBarcode = string.Empty;

                            try
                            {
                                    //User tap Annetna icon to get real time pricing information 
                                StaticHolder.DisPlay8Rows = true;
                                _ma.CheckPrice(OperationModeType.LV);
                            }
                            catch (Exception ex)
                            {
                                    StaticHolder.ReportCrash("btnRefresh", ex);
                            }

                            _txtBarCode.SelectAll();
                        }

                        break;
                    case Resource.Id._btnBuy:
                        _ma.AppendToFile(StaticHolder.BuyFile);
                        _ma._btnBuy.Visibility = ViewStates.Gone;
                        if (_ma._itemReturnInfo != null)
                        {
                            Item item = new Item();
                            item.Barcode = _ma._itemReturnInfo.InputBarcode;
                            item.ImageUrl = _ma._itemReturnInfo.ImageUrl;
                            //item.MinimumPrice = _ma._itemReturnInfo.AmazonPrice;
                            double lowest = 0.0;

                            OfferType[] array = _ma._itemReturnInfo.GetNewOffers();
                            foreach (OfferType offer in array)
                            {
                                double value;
                                System.Double.TryParse(offer.Price, out value);
                                if (value < lowest)
                                    lowest = value;
                            }

                            array = _ma._itemReturnInfo.GetUsedOffers();
                            foreach (OfferType offer in array)
                            {
                                double value;
                                System.Double.TryParse(offer.Price, out value);
                                if (value < lowest)
                                    lowest = value;
                            }

                            item.MinimumPrice = lowest.ToString();
                            item.SalesRank = _ma._itemReturnInfo.SalesRank;
                            item.Title = _ma._itemReturnInfo.Title;
                            BuyItemController.Instance.AddItem(item);
                            BuyItemController.Instance.SaveCategories();
                        }
                        break;
                    case Resource.Id.btnMulti:
                        _ma.ShowMultipleItems();
                        break;
                    case Resource.Id.btnCamel:
                            if (string.IsNullOrEmpty(StaticHolder.UpcInput))
                        {
                                //no barcode
                                break;
                        }

                            //todo has bugs
                            //  Intent camel = new Intent(_ma, typeof (CamelActivity));

                            Intent camel = new Intent(_ma, typeof (CamelWebViewActivity));

                            string code = StaticHolder.UpcInput;
                            Util.DoEan2Isbn(ref code);

                            camel.PutExtra("id", code);
                            _ma.StartActivity(camel);
                        break;
                        case Resource.Id.btnLink: //user click hot link
                        if (_txtBarCode.Text.Length == 0)
                            return;
                        AlertDialog.Builder builderSingle = new AlertDialog.Builder(_ma);
                        ArrayAdapter<string> arrayAdapter = new ArrayAdapter<String>(_ma,
                                                                                     Android.Resource.Layout
                                                                                            .SelectDialogSingleChoice,
                                                                                     _ma.Resources.GetTextArray(
                                                                                         Resource.Array.link_list));

                        builderSingle.SetSingleChoiceItems(arrayAdapter, 0,
                                                               (sender, e) =>
                                                               {
                                                                   string url = String.Empty;
                                                                       string barcode = GetBarcode();
                                                                   string title = String.Empty;
                                                                   if (!String.IsNullOrEmpty(barcode))
                                                                   {
                                                                       switch (e.Which)
                                                                       {
                                                                           case 0:
                                                                               url =
                                                                                   "http://bookscouter.com/prices.php?isbn=" +
                                                                                   barcode;
                                                                               title = "BookScouter";
                                                                               break;
                                                                           case 1:
                                                                               url =
                                                                                   "http://www.bookfinder.com/search/?isbn=&keywords=" +
                                                                                   barcode +
                                                                                   "&minprice=&maxprice=&mode=advanced&st=sr&ac=qr";
                                                                               title = "BookFinder";
                                                                               break;
                                                                           case 2:
                                                                               url =
                                                                                   "http://www.addall.com/New/submitNew.cgi?query=" +
                                                                                   barcode;
                                                                               title = "AddAll";
                                                                               break;
                                                                           case 3:
                                                                                   url = "http://search.ebay.com/" +
                                                                                         barcode;
                                                                               title = "Ebay";
                                                                               break;
                                                                           case 4:
                                                                                   url = string.Format(
                                                                                       "http://www.ebay.com/sch/i.html?_nkw={0}&LH_Sold=1&LH_Complete=1",
                                                                                       barcode);
                                                                                   title = "Ebay (Sold)";
                                                                               break;
                                                                           case 5:
                                                                               url =
                                                                                       string.Format(
                                                                                           "https://www.google.com/search?q={0}",
                                                                                           barcode);
                                                                                   title = "Google (UPC)";
                                                                               break;
                                                                           case 6:

                                                                                   //https://www.google.com/search?q=%22chicken+soup+for+the+soul%22&tbm=shop
                                                                               url =
                                                                                       string.Format(
                                                                                           "https://www.google.com/search?q={0}&tbm=shop",
                                                                                           WebUtility.UrlEncode(
                                                                                               _ma.GetTitle()));
                                                                                   title = "Google (Shopping)";
                                                                               break;
                                                                       }

                                                                           ((AlertDialog) sender).Dismiss();
                                                                       Intent i = new Intent(_ma,
                                                                                             typeof (
                                                                                                     CamelWebViewActivity
                                                                                                     ));
                                                                       i.PutExtra("url", url);
                                                                       i.PutExtra("title", title);
                                                                       _ma.StartActivity(i);
                                                                   }
                                                                   });
                        builderSingle.Show();
                        break;
                    case Resource.Id.btnGo:
                        Go();
                        break;
                    case Resource.Id.imageview:
                        if (_ma._itemReturnInfo != null && !string.IsNullOrEmpty(_ma._itemReturnInfo.ASIN))
                        {
                            String asin = _ma._itemReturnInfo.ASIN;

                            String amazonURL = "http://www.amazon.com/gp/offer-listing/"
                                               + asin
                                               // + "/ref=olp_sss_all?shipPromoFilter=1&sort=sip&condition=all";
                                               + "/ref=olp_sss_all?sort=sip&condition=all";
                            Intent intent = new Intent();

                            intent.SetAction(Intent.ActionView);
                                Uri contentUriBrowsers = Uri.Parse(amazonURL);
                            intent.SetData(contentUriBrowsers);
                                _ma.StartActivity(intent);
                            }

                        break;

                    case Resource.Id.numkeypad_back:
                        if (_barcode == null)
                        {
                            _barcode = "";
                            UpdateBarcode();
                            return;
                        }

                        if (_barcode.Length > 1)
                        {
                            _barcode = _barcode.Substring(0, _barcode.Length - 1);
                            UpdateBarcode();
                        }
                        else if (_barcode.Length == 1)
                        {
                            _barcode = "";
                            UpdateBarcode();
                        }
                        break;

                    case Resource.Id.numkeypad_cancel:
                        _barcode = string.Empty;
                        _txtBarCode.Text = string.Empty;
                        break;
                    case Resource.Id.btnCancelText:
                        _barcode = string.Empty;
                        _txtBarCode.Text = string.Empty;

                        _ma.ToggleNumPadVisibility();
                        _txtBarCode.SelectAll();
                        break;
                }
            }
                catch (Exception ex)
                {
                    StaticHolder.ReportCrash("OnClick", ex);
                }
            }

            public bool OnKey(View v, Keycode keyCode, KeyEvent e)
            {
                if (e.Action == KeyEventActions.Up && keyCode == Keycode.Menu)
                {
                    Intent i = new Intent(PreferenceClass.ApplicationContext, typeof (MenuActivity));
                    _ma.StartActivityForResult(i, RequestCodeForMenu);
                    return true;
                }
                if ((e.Action == KeyEventActions.Down)
                    && (keyCode == Keycode.Enter))
                {
                    _ma._btnGo.RequestFocus();

                    try
                    {
                        StaticHolder.DisPlay8Rows = PreferenceClass.LargerPricingRow;
                        _ma.CheckPrice(PreferenceClass.OperationMode);
                    }
                    catch (Exception ex)
                    {
                        StaticHolder.ReportCrash("OnKey", ex);
                    }
                    _txtBarCode.SelectAll();
                    return true;
                }
                return false;
            }

            public Boolean OnDoubleTap(MotionEvent e)
            {
                Intent i = new Intent(PreferenceClass.ApplicationContext, typeof (MenuActivity));
                _ma.StartActivityForResult(i, RequestCodeForMenu);
                return true;
            }

            public Boolean OnDoubleTapEvent(MotionEvent e)
            {
                return true;
            }

            public Boolean OnSingleTapConfirmed(MotionEvent e)
            {
                return true;
            }

            public Boolean OnDown(MotionEvent e)
            {
                return true;
            }

            public Boolean OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
            {
                return true;
            }

            public void OnLongPress(MotionEvent e)
            {
            }

            public Boolean OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
            {
                return true;
                //throw new NotImplementedException();
            }

            public void OnShowPress(MotionEvent e)
            {
            }

            public Boolean OnSingleTapUp(MotionEvent e)
            {
                return true;
            }
        }

        private void StartInputWorker(int count, long interval)
        {
            new Thread(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (_engine == null)
                            break;
                        CheckPrice(PreferenceClass.OperationMode);
                        Thread.Sleep(interval);
                        // DoDisplayResult(StaticHolder._returnInfos[3]);
                    }
                }).Start();
        }

        private static int _testcounter;

        private static readonly string[] TestBarcodes = new[]
            {
                "0884501226806",
                "9781579728304",
                "9780981741734",


             /*   "9780078778506",
"9780078794346", */
/*
                "9780805445527",
                "0345472403",
                "885370147100",
                "123456789",

                /*
                "9780345472403",
                "9780393972832",
                "9780582419476",
                "9781853267338",
                "9780140390469",
                "9780007129706",
                "123456789"
                 */
            };

        private void CheckPrice(OperationModeType operationMode)
        {
            // clear the content of the UI first.

            StopRefreshAnimation();

            if (string.IsNullOrEmpty(_txtBarCode.Text))
            {
                //no input barcode, return
                HideKeyBoard();
                _lastBarcode = string.Empty;
                _btnBuy.Visibility = ViewStates.Invisible;
                return;
            }
            try
            {
                // check if the user typed in the same barcode the second time. if 2nd time, start voice
                if (_txtBarCode.Text.Trim().Equals(_lastBarcode) && !_txtBarCode.Text.Trim().Equals("0") &&
                    (_itemReturnInfo != null))
                {
                    if (!PreferenceClass.DisableSound)
                    {
                        _mts.Speak(_engine.StartVoicePrompt(_itemReturnInfo), QueueMode.Flush,
                                   new Dictionary<string, string>());
                    }
                    _lastBarcode = string.Empty;
                    return;
                }


                //reset multi button to be invisible before next search
                _btnMulti.Visibility = ViewStates.Invisible;
                // _btnCamel.Visibility = ViewStates.Invisible;
                _btnBuy.Visibility = ViewStates.Invisible;

                StaticHolder.UpcInput = _txtBarCode.Text.Trim();

                if (operationMode == OperationModeType.LV)
                {
                    StartRefreshAnimation();
                }

                //this is async call
#if TEST_SERVER
				for (int i = 0; i < 50; i++)
#endif
                {
                    _engine.Lookup(StaticHolder.UpcInput, operationMode);

#if TEST_SERVER
					Thread.Sleep(1000);
#endif
                }
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                    {
                        _txtTitle.SetTextSize(ComplexUnitType.Sp, 11);
                        _txtTitle.Text = ex.Message;
                        StaticHolder.ReportCrash("CheckPrice", ex);
                        StopRefreshAnimation();
                    });
            }
        }

        private void DoDisplayResult(ItemReturnInfo returnInfo)
        {
            _txtTitle.SetTextColor(Color.Black);
            ItemResult itemResult = returnInfo.ItemResult;
            if ((itemResult.TriggerUsed == null) || (itemResult.TriggerUsed.Result == LookupReturnValue.Reject))
            {
                DoAudioNotification();
            }
            else
            {
                if (!PreferenceClass.DisableSound)
                {
                    //not disable sound
                    if (Util.IsSpecialPriceCase(itemResult.TriggerUsed.Result))
                    {
                        DoAudioNotification();
                    }
                    else
                    {
                        //Adv. custom trigger sound
                        _soundEngine.PlaySound((AudioFilesType) itemResult.TriggerUsed.AudioSound);
                    }
                }
            }

            //Setup Buy decision Text
            _txtBuy.SetTextColor(Color.Black);
            _txtBuy.Text = returnInfo.OutputText;

            if (string.IsNullOrEmpty(returnInfo.OutputText))
            {
                if (Util.AdvTriggerisNullOrReject(itemResult))
                {
                    //item is a reject or use basic trigger
                    _txtBuy.Text = Util.DisplayBuyDecisionText(returnInfo.Result);
                }
                else
                {
                    //use Adv. trigger
                    if (itemResult.TriggerUsed != null && Util.SpecialCase(itemResult.TriggerUsed.Result))
                    {
                        _txtBuy.Text = Util.DisplayBuyDecisionText(itemResult.TriggerUsed.Result);
                    }
                    else
                    {
                        if (itemResult.TriggerUsed != null) _txtBuy.Text = itemResult.TriggerUsed.Info;
                    }
                }
            }

            //set background color
            if (Util.AdvTriggerisNullOrReject(returnInfo.ItemResult))
            {
                //use normal decision background color
                SetDecisionBackgroundColor(returnInfo.Result);
            }
            else
            {
                SetDecisionBackgroundColor(returnInfo.ItemResult.TriggerUsed.Color);
            }

            //todo  test check metropcs, stellar and s3
            //set display resolution
            if (_txtBuy != null && _txtBuy.Visibility == ViewStates.Visible)
            {
                DisplayMetrics metrics = Resources.DisplayMetrics;
                switch (metrics.DensityDpi)
                {
                    case DisplayMetricsDensity.Low:
                    case DisplayMetricsDensity.Medium:
                        _txtBuy.SetTextSize(ComplexUnitType.Sp, 14);
                        break;
                }
            }
            // we have got the results from the Mockup
            // now validate and populate the data in the UI                
            _btnBuy.Visibility = ViewStates.Invisible;

            if (returnInfo.Found)
            {
                Color color = Color.Black;
                string title = returnInfo.Title;
                //string title = string.Format("{0} {1}", returnInfo.Title, Util.GetDescription(returnInfo.ItemResult.Media2));

                switch (returnInfo.Result)
                {
                    case LookupReturnValue.Buy:
                    case LookupReturnValue.FBABuy:
                    case LookupReturnValue.NoSellerListing:
                        //save the item to the buy file automatically
                        AppendToFile(StaticHolder.BuyFile);
                        break;
                    default:
                        //show the buy button, so user can choose to click to add to it.
                        _btnBuy.Visibility = ViewStates.Visible;
                        break;
                }
                Clear(title, !_txtBarCode.Text.Trim().Equals(_lastBarcode), color);

                _category = returnInfo.Category;

                //set Amazon price
                if ((returnInfo.OperateMode == OperateModeType.L0) || (string.IsNullOrEmpty(returnInfo.AmazonPrice)))
                {
                    _amazonPrice = string.Empty;
                }
                else
                {
                    _amazonPrice = returnInfo.AmazonPrice;
                }

                //set weight
                if ((returnInfo.OperateMode == OperateModeType.L0) || (string.IsNullOrEmpty(returnInfo.Weight)))
                {
                    _weight = string.Empty;
                }
                else
                {
                    _weight = returnInfo.Weight;
                }


                _salesRank = returnInfo.SalesRank == "0" ? string.Empty : returnInfo.SalesRank;

                //display item information on main UI
                ShowItemMetaData();

                if (string.IsNullOrEmpty(returnInfo.ImageUrl))
                {
                    _imageView.SetImageDrawable(Resources.GetDrawable(Resource.Drawable.noimage_amazon));
                }
                else
                {
                    if (!_txtBarCode.Text.Trim().Equals(_lastBarcode) || StaticHolder._returnInfos.Length > 1)
                    {
                        //Display product image for new input or input with multiple results (user go through the multiple results)
                        new DownloadImage(this, _imageView).Execute(returnInfo.ImageUrl);
                    }
                }
                // MEMORY LEAK - all leaks start here.
                AddRows(returnInfo);

                _itemReturnInfo = returnInfo;
            }
            else
            {
                //not found

                //string title = "<font color=\"#0000ff\">" + returnInfo.OutputText + "</font>";
                //string title = returnInfo.OutputText;
                Clear(string.Empty, true, Color.Blue);
                _itemReturnInfo = null;
            }
            _lastBarcode = _txtBarCode.Text.Trim();

            if ((PreferenceClass.VoiceOnly) && (!PreferenceClass.DisableSound))
            {
                //voice only and without disable the sound.

                if (_itemReturnInfo != null && (_itemReturnInfo.Found))
                {
                    _mts.Speak(_engine.StartVoicePrompt(_itemReturnInfo), QueueMode.Flush,
                               new Dictionary<string, string>());
                }
                else if (!string.IsNullOrEmpty(returnInfo.OutputText))
                {
                    _mts.Speak(returnInfo.OutputText, QueueMode.Flush,
                               new Dictionary<string, string>());
                }
            }

            // update the counter value
            //for demo user, initilize remaining 
            _txtCounter.Visibility = PreferenceClass.IsDemoUser ? ViewStates.Visible : ViewStates.Invisible;
            _txtCounter.Text = PreferenceClass.CheckCount.ToString(CultureInfo.CurrentCulture);
        }

        private void SetDecisionBackgroundColor(Color decision)
        {
            _txtBuy.Visibility = ViewStates.Visible;

            _txtBuy.SetBackgroundColor(decision);

            if (decision == Color.Black)
            {
                //if background is black, then set foreground text color to be white.
                _txtBuy.SetTextColor(Color.White);
            }
        }

        private void SetDecisionBackgroundColor(LookupReturnValue result)
        {
            _txtBuy.Visibility = ViewStates.Visible;

            if (Util.IsItemasBuy(result))
            {
                _txtBuy.SetBackgroundColor(Color.LightGreen);
            }
            else
            {
                switch (result)
                {
                    case LookupReturnValue.Reject:
                        _txtBuy.SetBackgroundColor(Color.Red);
                        break;
                    case LookupReturnValue.NotFound:
                        _txtBuy.SetBackgroundColor(Color.LightSkyBlue);
                        break;
                    case LookupReturnValue.WatchListing:
                        _txtBuy.SetBackgroundColor(Color.Magenta);
                        break;
                    default:
                        _txtBuy.SetBackgroundColor(Color.MediumVioletRed);
                        break;
                }
            }
        }

        private void StartRefreshAnimation()
        {
            if (!_btnRefresh.Enabled)
            {
                return;
            }

            _btnRefresh.Enabled = false;

            // Start the animation
            ((AnimationDrawable) _btnRefresh.Drawable).Start();
        }

        private void StopRefreshAnimation()
        {
            if (_btnRefresh.Enabled)
            {
                return;
            }
            // Enable back the TextViews so that they can be tapped upon again
            _btnRefresh.Enabled = true;

            // Stop the animation and reset the image
            ((AnimationDrawable) _btnRefresh.Drawable).Stop();
            ((AnimationDrawable) _btnRefresh.Drawable).Start();
            ((AnimationDrawable) _btnRefresh.Drawable).Stop();
        }

        private void ShowItemMetaData()
        {
            Dictionary<string, string> items = new Dictionary<string, string>();
            List<string> l = new List<string>();
            if (!string.IsNullOrEmpty(_category))
            {
                items.Add("category", _category);
            }
            else
            {
                items.Add("category", string.Empty);
            }
            l.Add("category");

            if (!string.IsNullOrEmpty(_amazonPrice))
            {
                items.Add("amazon_price", _amazonPrice);
            }
            else
            {
                items.Add("amazon_price", string.Empty);
            }
            l.Add("amazon_price");

            bool setWeight = false;
            if (!string.IsNullOrEmpty(_weight))
            {
                float result;
                if (Single.TryParse(_weight, NumberStyles.Number, CultureInfo.CurrentCulture, out result) &&
                    Math.Abs(result - 0) > 0.009)
                {
                    items.Add("weight", _weight);

                    setWeight = true;
                }
            }

            if (!setWeight)
            {
                items.Add("weight", string.Empty);
            }
            l.Add("weight");

            bool setRank = false;
            if (!string.IsNullOrEmpty(_salesRank))
            {
                ulong result;
                if ((UInt64.TryParse(_salesRank, NumberStyles.Number, CultureInfo.CurrentCulture, out result)) &&
                    (result > 0))
                {
                    items.Add("sales_rank", _salesRank);
                    setRank = true;
                }
            }

            if (!setRank)
            {
                items.Add("sales_rank", string.Empty);
            }
            l.Add("sales_rank");


            string[] vals = l.ToArray();

                    // Adding category at the end of the title
            _txtTitle.Text += GetItemMetaHtml(vals[0]);

            //set title font size based on number of letters
                    if (_txtTitle.Text.Length < 15)
                        _txtTitle.SetTextSize(ComplexUnitType.Sp, 22);
                    else if (_txtTitle.Text.Length < 25)
                        _txtTitle.SetTextSize(ComplexUnitType.Sp, 20);
                    else
                    {
                        _txtTitle.SetTextSize(ComplexUnitType.Sp, 16);
                    }
                    _txtAmazonPrice.TextFormatted = GetItemMetaHtml(vals[1]);
                    _txtWeight.TextFormatted = GetItemMetaHtml(vals[2]);
                    _txtSalesRank.TextFormatted = GetItemMetaHtml(vals[3]);
                    _txtSalesRank.SetTextSize(ComplexUnitType.Sp, 16);
        }

        private ISpanned GetItemMetaHtml(string str)
        {
            switch (str)
            {
                case "category":
                    ISpanned spanned = Html.FromHtml(" (" + _category + ")");
                    return spanned;

                case "amazon_price":
                    return string.IsNullOrEmpty(_amazonPrice)
                               ? Html.FromHtml("<font color=\"#2222ff\"> " + "</font>")
                               : Html.FromHtml("<font color=\"#2222ff\">AZ: $" + _amazonPrice + "</font>");
                case "weight":
                    return string.IsNullOrEmpty(_weight)
                               ? Html.FromHtml("<font color=\"#2222ff\">" + "</font>")
                               : Html.FromHtml("<font color=\"#2222ff\">WT: " + _weight + " lb</font>");

                case "sales_rank":

                    if (string.IsNullOrEmpty(_salesRank))
                    {
                        return
                            Html.FromHtml("<font color=\"#000000\"></font> " + "<font color=\"#2222ff\">" +
                                          string.Empty +
                                          "</font>");
                    }

                    ulong salesRank;

                    //shorten the sales rank length, display as 123, 1.23K, 1.23M
                    if (ulong.TryParse(_salesRank, NumberStyles.Number, CultureInfo.CurrentCulture, out salesRank))
                    {
                       

                        return Html.FromHtml("<font color=\"#000000\">Rank:</font> " + "<font color=\"#2222ff\">" +
                                                   _salesRank +
                                                   "</font>");
                    }
                    break;
            }
            return null;
        }

        private void ShowMultipleItems()
        {
            Intent it = new Intent(PreferenceClass.ApplicationContext, typeof (ItemsActivity));
            StartActivityForResult(it, RequestCodeForDisplayingMultipleItems);
        }

        //Add condition and number of offers following the prices
        private static string GetOfferSuffixValue(string condition, int offers, bool displayUsedCondition,
                                           bool displayNumberOfOffers)
        {
            string value = string.Empty;
            if (displayUsedCondition)
            {
                value = condition;
            }
            if (displayNumberOfOffers)
            {
                if (value.Length > 0)
                {
                    value = offers + value;
                }
                else
                {
                    value += offers;
                }
            }
            return value;
        }

        private TableLayout _tableLayout;
        private TableRow[] _trArray;
        private LinearLayout _table;
        private LinearLayout _scrollViewTableRow;
        private TableLayout _dynamicTable;

        private void AddRows(ItemReturnInfo returnInfo)
        {
            TableRow.LayoutParams lpProfit = null, lpFBA, lpNew, lpUsed;
            if (PreferenceClass.DisplayNetColumn)
            {
                lpProfit = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                     0.25f);
                lpFBA = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                  0.25f);
                lpNew = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                  0.25f);
                lpUsed = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                   0.25f);
                _txtProfitHeader.Visibility = ViewStates.Visible;
                _txtProfitTotal.Visibility = ViewStates.Visible;
            }
            else
            {
                lpFBA = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                  0.34f);
                lpNew = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                  0.33f);
                lpUsed = new TableRow.LayoutParams(0, ViewGroup.LayoutParams.WrapContent,
                                                   0.33f);
                _txtProfitHeader.Visibility = ViewStates.Gone;
                _txtProfitTotal.Visibility = ViewStates.Gone;
            }

            string[] profitOffers = returnInfo.GetProfitOffers();
            OfferType[] fbaOffers = returnInfo.GetFBAOffers();
            OfferType[] newOffers = returnInfo.GetNewOffers();
            OfferType[] usedOffers = returnInfo.GetUsedOffers();

            int numRows = profitOffers.Length > fbaOffers.Length
                              ? profitOffers.Length
                              : fbaOffers.Length;
            numRows = numRows > newOffers.Length ? numRows : newOffers.Length;
            numRows = numRows > usedOffers.Length ? numRows : usedOffers.Length;
            numRows = numRows < 8 ? numRows : 8;
            if (!StaticHolder.DisPlay8Rows && numRows > 5)
                numRows = 5;

            int maxLength = 0;

            int columnCount = PreferenceClass.DisplayNetColumn ? 4 : 3;

            if ((_trArray != null) && (_trArray[0].ChildCount !=columnCount ))
            {
                //bug fix:  _trArray can Not created ONCE, because user may switch from 3 column to 4 column (when enable show profit), in this case, we have to recreate the _trArray to hold the extra net column
                //todo verify by doing this, no memory leak here.
                DisposeMemoryForTable(ref _trArray);
            }

            if (_trArray == null)
            {
                AllocateMemoryForTable(out _trArray, 8, columnCount); 
            }
            for (int runs = 0; runs < numRows; runs++)
            {
                //to avoid array out of index case 348: https://racandroid.fogbugz.com/f/cases/348/
                if (runs >= _trArray.Length)
                {
                    break;
                }

                string col0 = profitOffers.Length > runs ? profitOffers[runs] : string.Empty;
                if (col0.Length > maxLength)
                    maxLength = col0.Length;
                string col1 = string.Empty;
                string offerv;
                if (runs < fbaOffers.Length)
                {
                    offerv = GetOfferSuffixValue(fbaOffers[runs].Condition, fbaOffers[runs].NumberOfOffersConsidered,
                                                 PreferenceClass.DisplayUsedCondition,
                                                 PreferenceClass.DisplayNumberOfOffers);
                    if (offerv.Length > 0)
                        offerv = fbaOffers[runs].Price + " (" + offerv + ")";
                    else
                        offerv = fbaOffers[runs].Price;
                    col1 = offerv;
                    if (col1.Length > maxLength)
                        maxLength = col1.Length;
                }

                string col2 = string.Empty;
                int count = StaticHolder.DisPlay8Rows ? 8 : 5;
                if (runs < usedOffers.Length && runs < count)
                {
                    offerv = GetOfferSuffixValue(usedOffers[runs].Condition, usedOffers[runs].NumberOfOffersConsidered,
                                                 PreferenceClass.DisplayUsedCondition,
                                                 PreferenceClass.DisplayNumberOfOffers);
                    if (offerv.Length > 0)
                        offerv = usedOffers[runs].Price + " (" + offerv + ")";
                    else
                        offerv = usedOffers[runs].Price;

                    col2 = offerv;
                    if (col2.Length > maxLength)
                        maxLength = col2.Length;
                }

                string col3 = string.Empty;
                if (runs < newOffers.Length)
                {
                    offerv = GetOfferSuffixValue("", newOffers[runs].NumberOfOffersConsidered, false,
                                                 PreferenceClass.DisplayNumberOfOffers);
                    if (offerv.Length > 0)
                    {
                        offerv = newOffers[runs].Price + " (" + offerv + ")";
                    }
                    else
                    {
                        offerv = newOffers[runs].Price;
                    }
                    col3 = offerv;
                    if (col3.Length > maxLength)
                        maxLength = col3.Length;
                }

                int index = 0;
                TextView view;
                if (PreferenceClass.DisplayNetColumn)
                {
                    view = (TextView) _trArray[runs].GetChildAt(index++);
                    view.SetTextColor(Color.Green);
                    view.LayoutParameters = lpProfit;
                    view.Text = col0;
                }

                try
                {
                    view = (TextView) _trArray[runs].GetChildAt(index++);
                    if ((runs < fbaOffers.Length) && (fbaOffers[runs].MultipleOffersAtThisPrice))
                        view.SetTextColor(Color.Rgb(170, 0, 170));
                    else
                        view.SetTextColor(Color.Blue);
                    view.LayoutParameters = lpFBA;
                    view.Text = col1;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, ex.Message);
                }

                view = (TextView) _trArray[runs].GetChildAt(index++);
                if ((runs < usedOffers.Length) && (usedOffers[runs].MultipleOffersAtThisPrice))
                    view.SetTextColor(Color.Rgb(170, 0, 170));
                else
                    view.SetTextColor(Color.Red);

                view.LayoutParameters = (lpUsed);
                view.Text = col2;
                view = (TextView) _trArray[runs].GetChildAt(index);
                try
                {
                    if ((runs < newOffers.Length) && (newOffers[runs].MultipleOffersAtThisPrice))
                        view.SetTextColor(Color.Rgb(170, 0, 170));
                    else
                        view.SetTextColor(Color.Green);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, ex.Message);
                }


                view.LayoutParameters = (lpNew);
                view.Text = col3;

                _tableLayout.AddView(_trArray[runs],
                                     new TableLayout.LayoutParams(ViewGroup.LayoutParams.FillParent,
                                                                  ViewGroup.LayoutParams.WrapContent));

                // change the visibility of the dynamic_table to visible
                _tableLayout.Visibility = ViewStates.Visible;

                _txtUsedTotal.Text = returnInfo.TotalUsed.ToString(CultureInfo.CurrentCulture);
                _txtNewTotal.Text = returnInfo.TotalNew.ToString(CultureInfo.CurrentCulture);


                int maxHeight = _table.Height - _trTitles.Height;
                int fontSize = EvaluateFontSize(maxHeight, _table.Width, maxLength);
                //Set font size

                _txtNewTotal.SetTextSize(ComplexUnitType.Px, fontSize);
                _txtUsedTotal.SetTextSize(ComplexUnitType.Px, fontSize);

                for (int i = 0, j = _tableLayout.ChildCount; i < j; i++)
                {
                    // then, you can remove the the row you want...
                    // for instance...
                    TableRow row = (TableRow) _tableLayout.GetChildAt(i);
                    for (int k = 0, n = row.ChildCount; k < n; k++)
                    {
                        view = (TextView) row.GetChildAt(k);
                        view.SetTextSize(ComplexUnitType.Px, fontSize);
                        view.SetSingleLine(true);
                        view.Gravity = (GravityFlags.Center | GravityFlags.Top);
                        if (n == 4 && k == 0)
                            view.SetBackgroundColor(Color.Rgb(201, 232, 255));
                        else
                        {
                            view.SetBackgroundColor(i%2 == 0 ? Color.White : Color.Rgb(240, 240, 240));
                        }
                        view.SetTypeface(null, TypefaceStyle.Bold);
                    }
                }
            }

            _scrollViewTableRow.LongClickable = true;
            _dynamicTable.Clickable = true;
            _dynamicTable.SetOnTouchListener(_onClickTouchListener);
        }


        private void AllocateMemoryForTable(out TableRow[] array, int rowsCount, int columnsCount)
        {
            array = new TableRow[rowsCount];
            for (int index = 0; index < rowsCount; index++)
            {
                array[index] = new TableRow(this);
                for (int i = 0; i < columnsCount; i++)
                {
                    TextView view = new TextView(this);
                    array[index].AddView(view);
                }
            }
        }

        private void DisposeMemoryForTable(ref TableRow[] array)
        {
            if (array != null)
            {
                try
                {
                    foreach (TableRow t in array)
                    {
                        for (int i = 0; i < t.ChildCount; i++)
                        {
                            t.RemoveViewAt(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    
                } 
                _trArray = null;
            }
        }

        private static int EvaluateFontSize(int maxHeight, int maxWidth, int maxColumnLength)
        {
            int maxLengthRow;
            if (PreferenceClass.DisplayNetColumn)
                maxLengthRow = maxColumnLength*4;
            else
                maxLengthRow = maxColumnLength*3;

            if (maxLengthRow == 0)
                return 0;
            int oneCharacterWidth = (int) (maxWidth/maxLengthRow);
            // to convert width to height we need to multiple width * 2;
            int size = oneCharacterWidth*2;

            //  + 1 ------> height, for total table row. 
            int rowsCount = 5 + 1;
            if (PreferenceClass.LargerPricingRow)
                rowsCount = 8 + 1;
            // 1.6 - free space
            while (rowsCount*size*1.6 > maxHeight)
                size--;
            return size;
        }

        private void Clear(String text, bool showStub, Color color)
        {
            if (showStub)
            {
                // MEMORY LEAK
                _imageView.SetImageResource(Resource.Drawable.stub);
                //_imageView.SetImageBitmap (BitmapFactory.DecodeResource (ApplicationContext.Resources,
                //                                                       Resource.Drawable.stub));
            }
            //_txtTitle.SetTextColor(ApplicationContext.Resources.GetColor(Resource.Color.sbc_page_number_text));
            _txtTitle.SetTextColor(color);
            _txtTitle.Text = text;
            //_txtTitle.TextFormatted = Html.FromHtml(text);
            //_txtCategory.Text = "";
            _txtWeight.Text = "";
            _txtSalesRank.Text = "";
            _txtAmazonPrice.Text = "";

            _txtFBATotal.Text = "";
            _txtUsedTotal.Text = "";
            _txtNewTotal.Text = "";


            TableLayout tl = (TableLayout) FindViewById(Resource.Id.dynamic_table);
            tl.RemoveAllViews();
        }

        private void LaunchRedLaser()
        {
			Intent intent = new Intent (this, typeof(ScanActivity));
			StartActivityForResult(intent, RequestCodeForScanning);
			//StartActivityForResult(_scanIntent, RequestCodeForScanning);
        }

        private void StartSearch(string text)
        {
            if (text.Trim().Equals(""))
            {
                Toast.MakeText(this, "Scanning cancelled.", ToastLength.Short).Show();
            }
            else
            {
                //Toast.MakeText(this, "Barcode found: " + text, ToastLength.Short).Show();
                _txtBarCode.Text = text.Trim();

                StaticHolder.DisPlay8Rows = PreferenceClass.LargerPricingRow;
                CheckPrice(PreferenceClass.OperationMode);
                _txtBarCode.SelectAll();
            }
        }

        private void ToggleNumPadVisibility()
        {
            if (_tl.Visibility == ViewStates.Invisible)
            {
                HideKeyBoard();
                _tl.Visibility = ViewStates.Visible;
                _txtBarCode.SelectAll();
            }
            else
            {
                _tl.Visibility = ViewStates.Invisible;
            }
        }

        private void HideKeyBoard()
        {
            InputMethodManager inputMgr = GetSystemService(InputMethodService) as InputMethodManager;

            if ((inputMgr != null) && (CurrentFocus != null))
            {
                inputMgr.HideSoftInputFromWindow(CurrentFocus.WindowToken, 0);
            }
        }

        private class KeypadClickListener : Java.Lang.Object, View.IOnClickListener
        {
            public void OnClick(View v)
            {
                //todo future work integrate vibrate
                //vibe.vibrate(25);
                for (int i = 0; i < 10; i++)
                {
                    if (Mykeys[i].Id == v.Id)
                    {
                        if (IsSelectAll())
                        {
                            _barcode = "";
                            _txtBarCode.Text = "";
                        }
                        _barcode += i + "";
                        UpdateBarcode();
                        break;
                    }
                }
            }
        }

        private static void UpdateBarcode()
        {
            _txtBarCode.Text = _barcode;
            _txtBarCode.SetSelection(0);
            Go();
        }

        private static Boolean IsSelectAll()
        {
            return ((_txtBarCode.SelectionStart == 0) && (_txtBarCode.SelectionEnd == _txtBarCode.Text.Length));
        }

        private void FindViews()
        {
            _tl = FindViewById(Resource.Id.numkeypad);
            Mykeys[0] = FindViewById(Resource.Id.numkeypad_0);
            Mykeys[1] = FindViewById(Resource.Id.numkeypad_1);
            Mykeys[2] = FindViewById(Resource.Id.numkeypad_2);
            Mykeys[3] = FindViewById(Resource.Id.numkeypad_3);
            Mykeys[4] = FindViewById(Resource.Id.numkeypad_4);
            Mykeys[5] = FindViewById(Resource.Id.numkeypad_5);
            Mykeys[6] = FindViewById(Resource.Id.numkeypad_6);
            Mykeys[7] = FindViewById(Resource.Id.numkeypad_7);
            Mykeys[8] = FindViewById(Resource.Id.numkeypad_8);
            Mykeys[9] = FindViewById(Resource.Id.numkeypad_9);
            Mykeys[10] = FindViewById(Resource.Id.numkeypad_cancel);
            Mykeys[11] = FindViewById(Resource.Id.numkeypad_back);
            _btnSearchText = (Button) FindViewById(Resource.Id.numkeypad_go);
            _btnCancelText = (Button) FindViewById(Resource.Id.btnCancelText);
        }

        private void SetListeners()
        {
            for (int i = 0; i < 10; i++)
            {
                Mykeys[i].SetOnClickListener(new KeypadClickListener());
            }
            Mykeys[10].SetOnClickListener(_onClickTouchListener);

            Mykeys[11].SetOnClickListener(_onClickTouchListener);
            _btnSearchText.SetOnClickListener(_onClickTouchListener);

            _btnCancelText.SetOnClickListener(_onClickTouchListener);
        }

        /// <summary>
        /// Save to the buy file
        /// </summary>
        /// <param name="file"></param>
        private void AppendToFile(string file)
        {
            //todo test verify buy window and buy file synchornization
            using (TextWriter w = new StreamWriter(file, true))
            {
                w.Write(StaticHolder.UpcInput);
                w.Write("\r\n");
                w.Flush();
            }
        }

        private static void Go()
        {
            //CheckPrice();
        }


        private void DisplayIntro()
        {
            bool intro = PreferenceClass.DontDisplayIntro;
            if (!intro)
            {
                IntroManager.Add(typeof (Intro));
                IntroManager.Add(typeof (IntroEnd));
                IntroManager.Start(this);
            }
        }

        private static string GetBarcode()
        {
            return _txtBarCode.Text;
        }

        private string GetTitle()
        {
            string text = _txtTitle.Text;
            string textWithoutCategory = text.Remove(text.LastIndexOf('('),
                                                     text.LastIndexOf(')') - text.LastIndexOf('(') + 1);
            return textWithoutCategory;
        }
    }

    internal class DownloadImage : AsyncTask<string, Integer, Drawable>
    {
        private readonly MainActivity _ma;
        private readonly WeakReference<ImageView> _ivWeakRef;
        private readonly int _size;

        public DownloadImage(MainActivity ma, ImageView iv)
        {
            _ma = ma;
            _ivWeakRef = new WeakReference<ImageView>(iv);
            _size = 0;
        }

        protected override void OnPostExecute(Drawable image)
        {
            try
            {
                if (image != null && _ivWeakRef != null)
                {
                    ImageView iv;
                    _ivWeakRef.TryGetTarget(out iv);
                    iv.SetImageDrawable(image);
                }
            }
            catch (Exception ex)
            {
                StaticHolder.ReportCrash("OnPostExecute", ex);
            }
            //if (_ma != null)
            //    _ma.HideProgressDialog();
        }

        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] native_parms)
        {
            return base.DoInBackground(native_parms);
        }

        protected override Drawable RunInBackground(params string[] @arg0)
        {
            if (arg0[0] == null)
            {
                return null;
            }

            Bitmap scaledImage = null;
            try
            {
                string url = arg0[0];

                if (_size == 0)
                {
                    Java.Net.URL aURL = new Java.Net.URL(url);
                    Java.Net.URLConnection conn = aURL.OpenConnection();
                    conn.Connect();
                    BitmapFactory.Options options = new BitmapFactory.Options();
                    Bitmap image = BitmapFactory.DecodeStream(conn.InputStream, null, options);
                    //conn.InputStream.Close();
                    float scaleFactor = Math.Min(((float) StaticHolder.MAX_WIDTH)
                                                 /image.Width, ((float) StaticHolder.MAX_HEIGHT)/image.Height);
                    Matrix scale = new Matrix();
                    scale.PostScale(scaleFactor, scaleFactor);
                    scaledImage = Bitmap.CreateBitmap(image, 0, 0, image.Width, image.Height,
                                                      scale, false);
                }
                else
                {
                    Java.Net.URL aURL = new Java.Net.URL(url);
                    Java.Net.URLConnection conn = aURL.OpenConnection();
                    conn.Connect();
                    BitmapFactory.Options options = new BitmapFactory.Options {InJustDecodeBounds = true};
                    BitmapFactory.DecodeStream(conn.InputStream, null, options);

                    int requiredSize = _size;
                    int widthTmp = options.OutWidth, heightTmp = options.OutHeight;
                    int scal = 1;
                    while (true)
                    {
                        if (widthTmp/2 < requiredSize || heightTmp/2 < requiredSize)
                            break;
                        widthTmp /= 2;
                        heightTmp /= 2;
                        scal *= 2;
                    }

                    BitmapFactory.Options o2 = new BitmapFactory.Options {InSampleSize = scal};
                    conn = aURL.OpenConnection();
                    conn.Connect();
                    var test = BitmapFactory.DecodeStream(conn.InputStream, null, o2);
                    return new BitmapDrawable(test);
                }
            }
            catch (NullReferenceException)
            {
                return null;
            }
            catch (Java.IO.IOException e)
            {
                e.PrintStackTrace();
            }
            // memory leak
            BitmapDrawable be = new BitmapDrawable(scaledImage);
            return be;
        }
    }
}