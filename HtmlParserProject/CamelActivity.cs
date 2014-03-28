using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Webkit;
using Java.Lang;
using Android.Graphics.Drawables;
using Android.Graphics;

using HtmlAgilityPack;

using System.Net;
using System.IO;
using System.Text;
using Android.Views;

using Android.Content;
using Android.Net;
using Android.Util;
using System.Collections.Generic;

namespace HtmlParserProject
{
	[Activity (Label = "CamelCamelCamel", MainLauncher = true, Icon = "@drawable/Icon")]			
	public class CamelActivity : Activity
	{
		public const float MAX_WIDTH = 200;
		public const float MAX_HEIGHT = 300;
		private ImageView diagramSmall;

		private TableLayout table1;
		private TableLayout table2;
		private TextView tvTable1;
		private TextView tvTable2;
		private ProgressBar progress;
		string id;
		string url = System.String.Empty;

		private RelativeLayout main;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.camel);
			// Create your application here

			diagramSmall = (ImageView)FindViewById (Resource.Id.diagramSmall);
			progress = (ProgressBar)FindViewById (Resource.Id.progress);
			main = (RelativeLayout)FindViewById (Resource.Id.mainLayout);

			table1 = (TableLayout)FindViewById (Resource.Id.table1);
			tvTable1 = (TextView)FindViewById (Resource.Id.tvTable1);
			table2 = (TableLayout)FindViewById (Resource.Id.table2);
			tvTable2 = (TextView)FindViewById (Resource.Id.tvTable2);

		//id = Intent.Extras.GetString ("id");
			id = "1935096567";
			if (!System.String.IsNullOrEmpty (id)) {
				// set action on camel icon
				url = "http://camelcamelcamel.com/product/" + id;
			}
		}

		

		private void OkClicked (object sender, DialogClickEventArgs e)
		{
			(sender as AlertDialog).Dismiss ();
			Finish ();
		}

		protected override void OnStart()
		{
			base.OnStart ();

			progress.Visibility = ViewStates.Visible;
			// check whether the network connection is available. If not - close the App.
			ConnectivityManager cm = (ConnectivityManager)GetSystemService (
				Android.Content.Context.ConnectivityService);
			if (cm.ActiveNetworkInfo == null) {
				AlertDialog.Builder builder = new AlertDialog.Builder (this);
				builder.SetTitle (Resource.String.no_connection_title);
				builder.SetMessage (Resource.String.no_connection_message);
				builder.SetPositiveButton (Resource.String.button_ok, OkClicked);
				var dialog = builder.Create ();
				dialog.Show ();
				return;
			}

			new Thread (
				() => {
				if (!System.String.IsNullOrEmpty (url))
					ParseTables (url);
				// url from
				url = "http://charts.camelcamelcamel.com/us/" + id + "/amazon.png?force=1&zero=0&w=725&h=440&desired=false&legend=1&ilt=1&tp=all&fo=0&lang=en";
				new DownloadImage (diagramSmall, progress).Execute (url);
			}).Start();
		}

        private void ParseTables (string filePath)
        {
            string response = LoadHTML (filePath);

			// error in site - duplicates of enclosing tags
			response = response.Replace ("</li></li>", "</li>");
            
			// this line exclude error when parsing
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove ("option");
            HtmlDocument htmlDoc = new HtmlDocument ();

            // There are various options, set as needed
            htmlDoc.OptionFixNestedTags = true;

			htmlDoc.LoadHtml (response);

            // ParseErrors is an ArrayList containing any errors from the Load statement
            if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count () > 0) {
                // Handle any parse errors as required
            } else {

                if (htmlDoc.DocumentNode != null) {
                    HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode ("//body");

                    if (bodyNode != null) {
                        RunOnUiThread (() => {
                            ParseFirstTable (htmlDoc, table1);
                            ParseSecondTable (htmlDoc, table2);
                        });
                    }
                }
            }
        }

		string LoadHTML (string filePath)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (filePath);
			request.Method = "GET";
			request.Credentials = CredentialCache.DefaultCredentials;

			HttpWebResponse response = (HttpWebResponse)request.GetResponse ();
			if (response != null) {
				var strReader = new StreamReader (response.GetResponseStream (), Encoding.UTF8);
				var responseToString = strReader.ReadToEnd ();
				return responseToString;
			}
			return null;
		}

        public void ParseFirstTable (HtmlDocument doc, TableLayout tableLayout)
        {
			// get Base Node of the tables 
            var baseNode = doc.GetElementbyId ("section_amazon");

            var node = doc.DocumentNode.SelectSingleNode ("//div[@class='pad']");
			// get text of the Table Header
            string text = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[1]/div/h3").InnerText;

            tvTable1.Text = text;
            tvTable1.Visibility = ViewStates.Visible;

			// Get Column names
            var thead = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[1]/div/table/thead/tr");
			// get all labels
            var labels = thead.ChildNodes.Where (x => x.Name == "th").ToArray ();
			 
			// create  LayoutParameters to fill 3 columns
            List<TableRow.LayoutParams> parameters = new List<TableRow.LayoutParams> {
                new TableRow.LayoutParams (0, ViewGroup.LayoutParams.WrapContent, 0.34f),
                new TableRow.LayoutParams (0, ViewGroup.LayoutParams.WrapContent, 0.33f),
                new TableRow.LayoutParams (0, ViewGroup.LayoutParams.WrapContent, 0.33f)
            };

            TableRow titles = new TableRow (this);
            int index = 0;
			// set each label to each column
            foreach (HtmlNode label in labels) {

                TextView column = new TextView (this);
                TableRow.LayoutParams parameter = parameters [index];
                parameter.SetMargins (8, 8, 8, 8);
                column.LayoutParameters = parameter;
                column.SetTextSize (Android.Util.ComplexUnitType.Sp, 16.0f);
                column.Text = label.InnerText;
                titles.AddView (column);
                column.SetTextColor (Color.Black);
                column.SetBackgroundColor (Color.LightGray);
                index ++;
            }
			// add table titles to the table
            tableLayout.AddView (titles);

			//. get body
            var tbody = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[1]/div/table/tbody");
            // get all rows 
			var rows = tbody.ChildNodes.Where (x => x.Name == "tr").ToArray ();

            int rowIndex = 0;
            foreach (HtmlNode row in rows) {
                TableRow tableRow = new TableRow (this);
                int i = 0;
                var columns = row.ChildNodes.Where (x => x.Name == "td").ToArray ();
                foreach (HtmlNode item in columns) {
                    TextView column = new TextView (this);
                    TableRow.LayoutParams parameter = parameters [i];
                    parameter.SetMargins (0, 4, 0, 4);
                    column.LayoutParameters = parameter;
                    column.Text = item.InnerText;
                    if (rowIndex == 1)
                        column.SetTextColor (Color.Red);
                    else if (rowIndex == 2)
                        column.SetTextColor (Color.Green);
                    else
                        column.SetTextColor (Color.Black);
                    tableRow.AddView (column);
                    i++;

                }

                View view = new View (this);
                view.SetBackgroundColor (Color.Black);
                view.LayoutParameters = new TableRow.LayoutParams (ViewGroup.LayoutParams.FillParent, 1);

                tableLayout.AddView (tableRow);
                tableLayout.AddView (view);
                rowIndex ++;
            }
        }

        public void ParseSecondTable (HtmlDocument doc, TableLayout tableLayout)
        {
            var baseNode = doc.GetElementbyId ("section_amazon");//  .Where(x => x.Name == "tr")

            var node = doc.DocumentNode.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[2]/div");
            string text = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[2]/div/h3").InnerText;

            tvTable2.Text = text;
            tvTable2.Visibility = ViewStates.Visible;

            var thead = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[2]/div/table/thead/tr");
            var labels = thead.ChildNodes.Where (x => x.Name == "th").ToArray ();

            List<TableRow.LayoutParams> parameters = new List<TableRow.LayoutParams> {
                new TableRow.LayoutParams (0, ViewGroup.LayoutParams.WrapContent, 0.7f),
                new TableRow.LayoutParams (0, ViewGroup.LayoutParams.WrapContent, 0.3f)
            };
            TableRow titles = new TableRow (this);
            int index = 0;
            foreach (HtmlNode label in labels) {
                TextView column = new TextView (this);
                TableRow.LayoutParams parameter = parameters [index];
                parameter.SetMargins (8, 8, 8, 8);
                column.LayoutParameters = parameter;
                column.SetTextSize (Android.Util.ComplexUnitType.Sp, 16.0f);
                column.Text = label.InnerText;
                titles.AddView (column);
                column.SetTextColor (Color.Black);
                column.SetBackgroundColor (Color.LightGray);
                index ++;
            }
            tableLayout.AddView (titles);

            var tbody = node.SelectSingleNode ("//*[@id=\"section_amazon\"]/div/div[2]/div/table/tbody");
            var rows = tbody.ChildNodes.Where (x => x.Name == "tr").ToArray ();


            foreach (HtmlNode row in rows) {
                TableRow tableRow = new TableRow (this);
                int i = 0;
                var columns = row.ChildNodes.Where (x => x.Name == "td").ToArray ();
                foreach (HtmlNode item in columns) {
                    TextView column = new TextView (this);
                    TableRow.LayoutParams parameter = parameters [i];
                    parameter.SetMargins (0, 4, 0, 4);
                    column.LayoutParameters = parameter;
                    column.Text = item.InnerText.Trim (new char[] { '\n', ' ' });
                    column.SetTextColor (Color.Black);
                    tableRow.AddView (column);
                    i++;
                }

                View view = new View (this);
                view.SetBackgroundColor (Color.Black);
                view.LayoutParameters = new TableRow.LayoutParams (ViewGroup.LayoutParams.FillParent, 1);

                tableLayout.AddView (tableRow);
                tableLayout.AddView (view);
            }
        }

internal class DownloadImage : AsyncTask<string, Integer, Drawable>
		{
			private readonly ImageView _iv;
			private readonly int _size;
			private readonly ProgressBar _progress;

			public DownloadImage (ImageView iv, ProgressBar progress)
			{
				_progress = progress;
				_iv = iv;
				_size = 0;
			}

			protected override void OnPostExecute (Drawable image)
			{
				try {
					if (image != null) {
						// set image to image view
						SetImage (image, _iv);
						// make progress bar invisible 
						_progress.Visibility = ViewStates.Invisible;
					}
				} catch (Java.Lang.Exception ex) {
					// report crash
				}
			}

			protected override Java.Lang.Object DoInBackground (params Java.Lang.Object[] native_parms)
			{
				return base.DoInBackground (native_parms);

			}

			protected override Drawable RunInBackground (params string[] @arg0)
			{
				if (arg0 [0] == null) {
					return null;
				}
				// 
				Bitmap scaledImage = null;
				try {
					string url = arg0 [0];

					if (_size == 0) {
						Java.Net.URL aURL = new Java.Net.URL (url);
						Java.Net.URLConnection conn = aURL.OpenConnection ();
						conn.Connect ();             
						BitmapFactory.Options options = new BitmapFactory.Options ();
						Bitmap image = BitmapFactory.DecodeStream (conn.InputStream, null, options);
//						//conn.InputStream.Close();
						float scaleFactor = System.Math.Min (MAX_WIDTH / image.Width, MAX_HEIGHT / image.Height);
						Matrix scale = new Matrix ();
						//scale.PostScale (scaleFactor, scaleFactor);
						scaledImage = Bitmap.CreateBitmap (image, 0, 0, image.Width, image.Height,
						                                   scale, false);
					} else {
						Java.Net.URL aURL = new Java.Net.URL (url);
						Java.Net.URLConnection conn = aURL.OpenConnection ();
						conn.Connect ();
						BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
						BitmapFactory.DecodeStream (conn.InputStream, null, options);

						int requiredSize = _size;
						int widthTmp = options.OutWidth, heightTmp = options.OutHeight;
						int scal = 1;
						while (true) {
							if (widthTmp / 2 < requiredSize || heightTmp / 2 < requiredSize)
								break;
							widthTmp /= 2;
							heightTmp /= 2;
							scal *= 2;
						}

						BitmapFactory.Options o2 = new BitmapFactory.Options { InSampleSize = scal };
						conn = aURL.OpenConnection ();
						conn.Connect ();
						var test = BitmapFactory.DecodeStream (conn.InputStream, null, o2);
						return new BitmapDrawable (test);
					}
				} catch (NullReferenceException) {
					return null;
				} catch (Java.IO.IOException e) {
					e.PrintStackTrace ();
				}
				BitmapDrawable be = new BitmapDrawable (scaledImage);
				return be;
			}

			public void SetImage (Drawable d, ImageView iv)
			{
				iv.SetImageDrawable (d);
			}
		}
	}
}

