using System;
using System.Collections.Generic;
using Android.Content;
using Android.Widget;
using Android.Graphics;
using System.Threading;
using System.IO;

namespace AmazonPriceChecker_mono
{
    using System.Globalization;

    public class ImageLoader
    {
        private readonly MemoryCache _memoryCache = new MemoryCache();
        private readonly FileCache _fileCache;
        private readonly Dictionary<ImageView, String> _imageViews = new Dictionary<ImageView, String>();
    //ExecutorService executorService;

        public ImageLoader(Context context)
        {
            _fileCache = new FileCache(context);
        //executorService=Executors.newFixedThreadPool(5);
    }

        private const int StubID = Resource.Drawable.noimage_amazon;

        private Bitmap GetBitmap(String url)
        {
            try
    {
            var imageUrl = new Java.Net.URL(url);
            using (Stream stream = imageUrl.OpenStream())
            {
                    String filename = url.GetHashCode().ToString(CultureInfo.InvariantCulture);
            String fileExt = url.Substring(url.LastIndexOf('.') + 1);

                string pathToFile = System.IO.Path.Combine(
                    Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, filename + "." + fileExt);
                    using (
                        var fileStream = new FileStream(pathToFile, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                byte[] buf = new byte[1024];
                int r;
                while ((r = stream.Read(buf, 0, buf.Length)) > 0)
                    fileStream.Write(buf, 0, r);
            }
            return decodeFile(new Java.IO.File(pathToFile));
            }
        }
        catch (Exception ex)
        {
                System.Diagnostics.Debug.Assert(false, ex.Message);
                return null;
        }
    }


    //decodes image and scales it to reduce memory consumption
        private Bitmap decodeFile(Java.IO.File f)
        {
        try
        {
            //decode image size
                BitmapFactory.Options o = new BitmapFactory.Options {InJustDecodeBounds = true};
                using (FileStream stream = File.OpenRead(f.AbsolutePath))
            {
            BitmapFactory.DecodeStream(stream, null, o);

            //Find the correct scale value. It should be the power of 2.
                    const int requiredSize = 70;
                    int widthTmp = o.OutWidth, heightTmp = o.OutHeight;
            int scale=1;
                while (true)
                {
                        if (widthTmp/2 < requiredSize || heightTmp/2 < requiredSize)
                    break;
                        widthTmp /= 2;
                        heightTmp /= 2;
                scale*=2;
            }

            //decode with inSampleSize
                    BitmapFactory.Options o2 = new BitmapFactory.Options {InSampleSize = scale};
                    using (FileStream stream2 = File.OpenRead(f.AbsolutePath))
                {
                    var test = BitmapFactory.DecodeStream(stream2, null, o2);
            return test;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Assert(false, ex.Message);
            
        }
        return null;
    }

    //Task for the queue
    public class PhotoToLoad
    {
            public String URL;
            public ImageView ImageView;

            public PhotoToLoad(String u, ImageView i)
            {
                URL = u;
                ImageView = i;
        }
    }

   public class PhotosLoader 
        //implements Runnable 
    {
            private readonly PhotoToLoad _photoToLoad;
            private readonly ImageLoader _il;
            public Bitmap Bmp;

            public PhotosLoader(PhotoToLoad photoToLoad, ImageLoader il)
            {
                _photoToLoad = photoToLoad;
            _il = il;
        }

            public void Run()
            {
                if (_il.ImageViewReused(_photoToLoad))
                return;
                Bmp = _il.GetBitmap(_photoToLoad.URL);
                _il._memoryCache.Put(_photoToLoad.URL, Bmp);
                if (_il.ImageViewReused(_photoToLoad))
                return;
            //BitmapDisplayer bd=new BitmapDisplayer(bmp, photoToLoad,_il);
                if (_il.ImageViewReused(_photoToLoad))
                return;
                if (Bmp != null)
                    _photoToLoad.ImageView.SetImageBitmap(Bmp);
            else
                    _photoToLoad.ImageView.SetImageResource(StubID);
                        
            //a.RunOnUiThread(() => StartBDThread(bd));
        }

            public void StartBdThread(BitmapDisplayer bd)
            {
                Thread bdThread = new Thread(bd.Run);
                bdThread.Start();
        }

        public void ThreadPoolCallback(Object threadContext)
        {
        }
    }

        private bool ImageViewReused(PhotoToLoad photoToLoad)
        {
            if (_imageViews.Count == 0)
            return false;

            String tag = _imageViews[photoToLoad.ImageView];
            if (tag == null || !tag.Equals(photoToLoad.URL))
            return true;
        return false;
    }

    //Used to display bitmap in the UI thread
   public class BitmapDisplayer
    {
            private readonly Bitmap _bitmap;
            private readonly PhotoToLoad _photoToLoad;
            private readonly ImageLoader _il;

            public BitmapDisplayer(Bitmap b, PhotoToLoad p, ImageLoader il)
            {
                _bitmap = b;
                _photoToLoad = p;
                _il = il;
            }
             
            public void Run()
        {
                if (_il.ImageViewReused(_photoToLoad))
                return;
                if (_bitmap != null)
                    _photoToLoad.ImageView.SetImageBitmap(_bitmap);
            else
                    _photoToLoad.ImageView.SetImageResource(StubID);
        }
    }

        public void ClearCache()
        {
            _memoryCache.Clear();
            _fileCache.Clear();
    }

}

    public class MemoryCache
    {
        private readonly Dictionary<String, WeakReference> _cache = new Dictionary<String, WeakReference>();

        public Bitmap Get(String id)
        {
            if (!_cache.ContainsKey(id))
                return null;
            WeakReference val;
            _cache.TryGetValue(id, out val);
            WeakReference wref = val;
            if (wref != null) return (Bitmap) wref.Target;
            return null;
        }

        public void Put(String id, Bitmap bitmap)
        {
            _cache.Add(id, new WeakReference(bitmap));
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }


    public class FileCache
    {
        private readonly Java.IO.File _cacheDir;

        public FileCache(Context context)
        {
        //Find the dir to save cached images
        if (Android.OS.Environment.ExternalStorageState.Equals(Android.OS.Environment.MediaMounted))
                _cacheDir = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory, "LazyList");
        else
                _cacheDir = context.CacheDir;
            if (!_cacheDir.Exists())
                _cacheDir.Mkdirs();
    }

        public Java.IO.File GetFile(String url)
        {
        //I identify images by hashcode. Not a perfect solution, good for the demo.
            String filename = url.GetHashCode().ToString(CultureInfo.InvariantCulture);
        //Another possible solution (thanks to grantland)
        //String filename = URLEncoder.encode(url);
        String fileExt = url.Substring(url.LastIndexOf('.') + 1);
            Java.IO.File f = new Java.IO.File(_cacheDir, filename + "." + fileExt);
        return f;

    }

        public void Clear()
        {
            Java.IO.File[] files = _cacheDir.ListFiles();
        foreach (Java.IO.File f in files)
            f.Delete();
    }
}

}