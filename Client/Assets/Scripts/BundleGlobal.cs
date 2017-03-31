using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Threading;

public class BundleItem
{
    public string bundlePath;
    public string localPath;
    public string serverPath;
    public uint version;
    public uint compress_crc;
    public bool isScene;
    public bool isGlobal;
    public bool isPushed = false;
    public bool isCompress = true;
    public int[] dependency;
    public string[] dependStr;

    /*public int pushCount = 0;
	
	public void PopDependency()
	{
		for(int i = 0; i < pushCount; i++)
		{
			BuildPipeline.PopAssetDependencies();
		}
	}*/
}

public class ResLoadParams
{
    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;
    public string info;
    //public sdMainChar mainChar;
    //public sdGameLevel level;
    public sdResourceMgr resourceMgr;
    public object userdata0;
    public object userdata1;
    public object userdata2;
    public object userdata3;
    public object userdata4;
    public object userdata5;
    public uint _reqIndex;  //请求时间 请求者不需要填写..
    public int petInt;
    public string petData0;
    public string petData1;
}

public delegate void ResLoadDelegate(ResLoadParams param, Object obj);
public class LoadRequest
{
    public ResLoadDelegate callbackFunc = null;
    public ResLoadParams param;
    public string path;
    public System.Type resType;
    public string resName;
    public string bundleName;
    public int priority = 0;
    public bool isScene = false;
    public void DoCallBack(Object obj)
    {
        if (callbackFunc != null)
        {
            if (obj == null)
            {
                Debug.Log(path + " Load Failed!");
                //return;
            }
            callbackFunc(param, obj);
        }
    }
}


public class BundleGlobalItem
{
    public BundleItem itemInfo;
    public AssetBundle bundle;
    public bool dontUnload = false;
    public int refCount = 0;

    public bool loading = false;
    public List<LoadRequest> lstCB = new List<LoadRequest>();
}
public class UnCompressTask
{
    public BundleGlobalItem item;
    public byte[] data;
}
/// <summary>
/// Be aware this will not prevent a non singleton constructor5.
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static object _lock = new object();
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                if (!BundleGlobal.IsMobile())
                {
                    //Debug.LogWarning("[Singleton] Instance " + typeof(T) +
                    //    " already destroyed on application quit." +
                    //    "Won't create again - returning null.");
                }
                return null;
            }
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();
                        DontDestroyOnLoad(singleton);
                        if (!BundleGlobal.IsMobile())
                        {
                            //Debug.Log("[Singleton] An instance of " + typeof(T) +
                            //    " is needed in the scene, so '" + singleton +
                            //    "' was created with DontDestroyOnLoad.");
                        }
                    }
                    else
                    {
                        if (!BundleGlobal.IsMobile())
                        {
                            //Debug.Log("[Singleton] Using instance already created: " +_instance.gameObject.name);
                        }
                    }
                }
                return _instance;
            }
        }
    }

    private static bool applicationIsQuitting = false;
    /// <summary>
    /// When unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.    
    /// If any script calls Instance after it have been destroyed,     
    ///   it will create a buggy ghost object that will stay on the Editor scene    
    ///   even after stopping playing the Application. Really bad!    
    /// So, this was made to be sure we're not creating that buggy ghost object.    
    /// </summary>
    public void OnDestroy()
    {
        if (ClearMode == false)
            applicationIsQuitting = true;
        ClearMode = false;
    }

    private static bool ClearMode = false;
    public static void DestorySingleton()
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                ClearMode = true;
                GameObject.Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}

public class BundleGlobal : Singleton<BundleGlobal>
{

    //static public BundleGlobal instance;
    static public BundleGlobalItem[] bundles = null;
    static public Hashtable bundleTable = new Hashtable();

    public string thePath;
    public int needDownLoadNum = 0;
    public int downloadedNum = 0;
    public static bool bundleTest = false;
    public static bool InternalNetDebug = false;
    public bool loading = false;
    public string infoPath;
    public string version = "";
    public string[] cdn;
    int cdnIndex = 0;

    WWW wwwPercent = null;
    int currentDownload = 1;
    int totalDownload = 1;
    public bool updating = false;

    static Dictionary<string, string> ReadINI(string content)
    {
        Dictionary<string, string> table = new Dictionary<string, string>();

        if (content.Length == 0)
        {
            return table;
        }
        string[] lines = content.Replace("\r", "").Split('\n');


        foreach (string s in lines)
        {
            if (s.Length > 0)
            {
                string[] element = s.Split('=');
                char[] data1 = element[0].ToCharArray();
                table[element[0]] = element[1];
            }
        }
        return table;
    }
    static Dictionary<string, string> __AppVersion = null;
    public static Dictionary<string, string> AppVersion()
    {
        if (__AppVersion == null)
        {
            TextAsset txtFile = Resources.Load("AppVersion") as TextAsset;
            __AppVersion = ReadINI(txtFile.text);
        }
        return __AppVersion;
    }
    public static bool IsMobile()
    {
        return Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.WP8Player ||
                bundleTest;
    }
    private List<BundleGlobalItem> loadingObjsFast = new List<BundleGlobalItem>();
    private List<BundleGlobalItem> loadingObjs = new List<BundleGlobalItem>();
    private List<BundleGlobalItem> loadingObjsSlow = new List<BundleGlobalItem>();
    static public void unloadBundle()
    {
        if (bundles != null)
        {
            for (int i = 0; i < bundles.Length; i++)
            {
                if ((bundles[i] != null) && (bundles[i].bundle != null))
                {
                    bundles[i].bundle.Unload(true);
                }
            }
        }
        bundles = null;
    }

    public AssetBundle FindAssetBundle(string url)
    {
        for (int i = 0; i < bundles.Length; i++)
        {
            if (bundles[i] != null)
            {
                if (bundles[i].itemInfo.bundlePath == url)
                {
                    return bundles[i].bundle;
                }
            }
        }

        return null;
    }

    public void UnloadNormalBundle(Hashtable table)
    {
        for (int i = 0; i < bundles.Length; i++)
        {
            if (bundles[i] == null)
            {
                continue;
            }
            if ((!bundles[i].itemInfo.isGlobal) &&
                (bundles[i].bundle != null))
            {

                if (bundles[i].dontUnload)
                {
                    continue;
                }
                if (table != null)
                {
                    if (table.ContainsKey(bundles[i]))
                    {
                        continue;
                    }
                }

                if (bundles[i] != null)
                {
                    bundles[i].bundle.Unload(false);
                    bundles[i].bundle = null;
                }
            }
        }
    }
    public static void ClearBundleDontUnloadFlag()
    {
        if (bundles != null)
        {
            for (int i = 0; i < bundles.Length; i++)
            {
                bundles[i].dontUnload = false;
            }
        }
    }


    //public void StartLoadBundleLevel(string bundlePath, string sceneName)
    //{
    //    sdUICharacter.Instance.DialogueCharacterList.Clear();
    //    //Debug.LogWarning("loadlevel");
    //    LoadLevel(bundlePath, sceneName);
    //}

    //public void StartUpdateAllBundles()
    //{
    //    if (!updating)
    //    {
    //        updating = true;
    //    }
    //    else
    //    {
    //        return;
    //    }

    //    if (IsMobile())
    //    {
    //        downloadedNum = 0;
    //        needDownLoadNum = 1;
    //        StartCoroutine(UpdateAllBundle());
    //    }
    //    else
    //    {
    //        OnDownloadFinished();
    //    }
    //}


    void Update()
    {
    }
    BundleGlobalItem PopLoadItem()
    {
        BundleGlobalItem item = null;
        if (loadingObjsFast.Count > 0)
        {
            item = loadingObjsFast[0];
            loadingObjsFast.RemoveAt(0);
        }
        else if (loadingObjs.Count > 0)
        {
            item = loadingObjs[0];
            loadingObjs.RemoveAt(0);
        }
        else if (loadingObjsSlow.Count > 0)
        {
            item = loadingObjsSlow[0];
            loadingObjsSlow.RemoveAt(0);

        }

        return item;
    }
    IEnumerator updateLoad()
    {
        while (true)
        {
            if (loadingObjs.Count == 0 && loadingObjsFast.Count == 0 && loadingObjsSlow.Count == 0)
            {
                yield return 0;
            }
            else
            {
                BundleGlobalItem item = PopLoadItem();
                if (item.itemInfo == null)
                {
                    item.loading = false;
                    foreach (LoadRequest obj in item.lstCB)
                    {
                        obj.callbackFunc(null, null);
                    };
                    item.lstCB.Clear();
                    yield return 0;
                    continue;
                }

                if (item.itemInfo.isScene)
                {
                    Hashtable UnloadTable = new Hashtable();
                    UnloadTable.Add(item, 0);


                    if (item.itemInfo.dependency != null)
                    {
                        foreach (int dependId in item.itemInfo.dependency)
                        {
                            UnloadTable.Add(bundles[dependId], 0);
                        }
                    }

                    UnloadNormalBundle(UnloadTable);

                    if (item.itemInfo.dependency != null)
                    {
                        foreach (int dependId in item.itemInfo.dependency)
                        {
                            if (bundles[dependId].bundle == null)
                            {
                                yield return 0;
                            }
                            LoadBundle(dependId);

                        }
                    }
                    string bundleName = item.lstCB[0].bundleName;
                    int i = (int)bundleTable[item.itemInfo.bundlePath];
                    if (bundles[i].bundle == null)
                    {
                        yield return 0;
                    }
                    LoadBundle(i);

                    string sceneName = item.lstCB[0].resName;
                    item.lstCB.Clear();
                    AsyncOperation op = Application.LoadLevelAsync(sceneName);
                    yield return op;
                    item.loading = false;

                }
                else
                {
                    if (item != null)
                    {


                        if (item.itemInfo.dependency != null)
                        {
                            foreach (int dependId in item.itemInfo.dependency)
                            {
                                bool bSkip = false;
                                if (bundles[dependId].bundle == null)
                                {
                                    bSkip = true;
                                }
                                LoadBundle(dependId);
                                if (bSkip)
                                {
                                    yield return 0;
                                }
                            }
                        }
                        if (item.bundle == null)
                        {
                            string bundlename = item.itemInfo.bundlePath;
                            item.bundle = AssetBundle.LoadFromFile(FixedPath(bundlename));
                            yield return 0;
                        }
                        item.loading = false;

                        while (item.lstCB.Count > 0)
                        {
                            LoadRequest obj = item.lstCB[0];
                            item.lstCB.RemoveAt(0);

                            Object callback_obj = null;
                            string path = obj.path;
                            System.Type resType = obj.resType;

                            if (obj.resType == typeof(AssetBundle))
                            {
                                // 尝试载入这个BUNDLE的所有资源，避免在使用时加载.
                                //item.bundle.LoadAll();
                                continue;
                            }

                            if (obj.priority == 1)
                            {
                                callback_obj = item.bundle.LoadAsset(obj.resName, resType);
                            }
                            else
                            {
                                AssetBundleRequest req = item.bundle.LoadAssetAsync(obj.resName, resType);
                                yield return req;
                                callback_obj = req.asset;
                            }
                            obj.DoCallBack(callback_obj);
                            if (obj.priority != 1)
                            {
                                yield return 0;
                            }
                        };

                    }
                }

            }
        }
    }
    static IEnumerator DoLoadNull(ResLoadDelegate cb)
    {
        yield return 0;
        yield return 0;

        cb(null, null);
    }
    public void LoadNull(ResLoadDelegate cb)
    {
        if (IsMobile())
        {
            LoadRequest loadobj = new LoadRequest();
            loadobj.callbackFunc = cb;
            BundleGlobalItem item = new BundleGlobalItem();
            item.itemInfo = null;

            item.lstCB.Add(loadobj);

            item.loading = true;
            loadingObjs.Add(item);
        }
        else
        {
            StartCoroutine(DoLoadNull(cb));
        }

    }
    public void LoadObject(LoadRequest loadobj)
    {
        if (IsMobile())
        {
            if (!bundleTable.ContainsKey(loadobj.bundleName))
            {
                Debug.Log("Bundle Don't Exist" + loadobj.bundleName + loadobj.path);
                return;
            }
            int index = (int)bundleTable[loadobj.bundleName];
            BundleGlobalItem item = bundles[index];
            if (item == null)
            {
                Debug.Log("Load Object Failed!=" + loadobj.path);
                return;
            }
            if (loadobj.isScene)
            {
                item.lstCB.Add(loadobj);
                item.loading = true;
                loadingObjs.Add(item);
            }
            else
            {
                if (item.bundle != null && loadobj.priority == 1)
                {
                    Object callback_obj = null;
                    string path = loadobj.path;
                    System.Type resType = loadobj.resType;
                    if (loadobj.resType != typeof(AssetBundle))
                    {
                        callback_obj = item.bundle.LoadAsset(loadobj.resName, resType);
                    }
                    loadobj.DoCallBack(callback_obj);
                }
                else
                {
                    item.lstCB.Add(loadobj);
                    if (!item.loading)
                    {
                        item.loading = true;
                        if (loadobj.priority == 1)
                        {
                            loadingObjsFast.Add(item);
                        }
                        else if (loadobj.priority == 0)
                        {
                            loadingObjs.Add(item);
                        }
                        else
                        {
                            loadingObjsSlow.Add(item);
                        }
                    }
                }
            }
        }
        else
        {
            if (loadobj.isScene)
            {
                string bundlePath = loadobj.bundleName;
                //检查手机加载路径的正确性，如果错误 日志提示..
                if (!bundlePath.EndsWith(".unity3d"))
                {
                    Debug.LogError("Level Path Isn't correct!(" + bundlePath + ")!");
                }
                else
                {
                    string strPath = bundlePath.Replace(".unity3d", "");

                    Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/" + strPath, typeof(Object));
                    if (obj == null)
                    {
                        Debug.LogError("Level Path Isn't correct!(" + bundlePath + ")!");
                    }
                    else
                    {
                        Resources.UnloadAsset(obj);
                    }
                }
                Application.LoadLevel(loadobj.resName);
            }
            else
            {
                if (loadobj.resType != typeof(AssetBundle))
                {
                    StartCoroutine(DoLoadFromFile(loadobj));
                }
            }
        }
    }
    IEnumerator DoLoadObject(LoadRequest req, Object obj)
    {
        yield return 0;
        req.DoCallBack(obj);
    }
    public void LoadLevel(string bundleName, string levelName)
    {
        LoadRequest loadobj = new LoadRequest();
        loadobj.isScene = true;
        loadobj.resName = levelName;
        loadobj.bundleName = bundleName;
        loadobj.path = bundleName;
        LoadObject(loadobj);
    }
    void OnApplicationQuit()
    {
        Debug.Log("Unload All Bundles");
        unloadBundle();
        //SDNetGlobal.disConnect();
    }

    static IEnumerator DoLoadFromFile(LoadRequest req)
    {
        yield return null;
        Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/" + req.path, req.resType);
        req.DoCallBack(obj);
    }
    public static void SetBundleDontUnload(string bundleName)
    {
        if (!bundleTable.ContainsKey(bundleName))
        {
            return;
        }
        BundleGlobalItem item = bundles[(int)bundleTable[bundleName]];

        item.dontUnload = true;
        if (item.itemInfo.dependency != null)
        {
            foreach (int i in item.itemInfo.dependency)
            {
                BundleGlobalItem depend = bundles[i];
                depend.dontUnload = true;
            }
        }

        LoadRequest req = new LoadRequest();
        req.bundleName = bundleName;
        req.resType = typeof(AssetBundle);
        BundleGlobal.Instance.LoadObject(req);

    }
    static string __localPath = "";
    public static string LocalPath
    {
        get
        {
            if (__localPath.Length == 0)
            {
                //IOS 审核不允许将网络资源放到Documents目录..
                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    __localPath = Application.persistentDataPath.Replace("Documents", "tmp") + "/";
                }
                else
                {
                    __localPath = Application.persistentDataPath + "/";
                }
            }
            return __localPath;
        }
    }
    public int GetBundlePercent()
    {
        if (wwwPercent != null)
        {
            return (int)(wwwPercent.progress * 100.0f);
        }
        return 100;
    }
    protected virtual void OnDownload(int i, int total)
    {
        //guiText.text	=	"("+idx+"/"+lstBundleInfo.Count+")"+str;
        downloadedNum = i;
        needDownLoadNum = total;
    }
    void OnDownloadFinished()
    {
        updating = false;
        downloadedNum = needDownLoadNum;
        //更新完成之后，再所有bundle加载之前.必须加载shaderlib...
        LoadBundle("shaderLib.unity3d");
    }

    void ChangeCDN()
    {
        cdnIndex++;
        if (cdnIndex >= cdn.Length)
        {
            cdnIndex = 0;
        }
    }
    string GetServer()
    {
        if (InternalNetDebug)
        {
            return "f:/unity/" + version;
        }
        return cdn[cdnIndex] + version;
    }


    void ParseBundleCSV(string[] lines, ref BundleGlobalItem[] array, Hashtable index)
    {
        if (lines.Length == 0)
        {
            return;
        }
        int iBundleCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string str = lines[i];
            if (str.Length == 0)
            {
                continue;
            }
            iBundleCount++;
        }
        array = new BundleGlobalItem[iBundleCount];

        for (int i = 1; i < lines.Length; i++)
        {
            string str = lines[i];
            if (str.Length == 0)
            {
                continue;
            }
            string[] element = str.Split(',');

            BundleGlobalItem item = new BundleGlobalItem();
            item.itemInfo = new BundleItem();
            item.itemInfo.bundlePath = element[1];
            item.itemInfo.localPath = element[2];
            item.itemInfo.version = uint.Parse(element[3]);
            item.itemInfo.compress_crc = uint.Parse(element[4]);
            item.itemInfo.isScene = element[5] == "1";
            item.itemInfo.isGlobal = element[6] == "1";
            item.itemInfo.isCompress = element[7] == "1";
            string depend = element[8];
            if (depend.Length > 0)
            {
                string[] dependlist = depend.Split(';');
                item.itemInfo.dependency = new int[dependlist.Length];
                for (int j = 0; j < dependlist.Length; j++)
                {
                    if (dependlist[j].Length > 0)
                    {
                        item.itemInfo.dependency[j] = int.Parse(dependlist[j]);
                    }
                    else
                    {
                        item.itemInfo.dependency[j] = 0;
                    }
                }
            }
            //item.itemInfo.serverPath = xmlElement.GetAttribute("server");
            int idx = i - 1;
            array[idx] = item;
            if (index != null)
            {
                index[item.itemInfo.bundlePath] = idx;
            }

        }
    }

    string FixedPath(string path)
    {
        return LocalPath + path.Replace("/", "__");
    }
    string strVersionFile;
    List<UnCompressTask> lstTask = new List<UnCompressTask>();
    object UnCompressLock = new object();
    bool ExitUnCompress = false;

    //protected void UnCompressMain()
    //{
    //    Debug.Log("BeginUnCompress");
    //    while (true)
    //    {
    //        UnCompressTask task = null;
    //        lock (UnCompressLock)
    //        {
    //            if (lstTask.Count > 0)
    //            {
    //                //Debug.Log("Task Count "+lstTask.Count);
    //                task = lstTask[0];
    //                lstTask.RemoveAt(0);
    //            }
    //        }
    //        if (task != null)
    //        {
    //            SaveFile(task.data, task.item.itemInfo);
    //            //保存当前已经下载的文件
    //            strVersionFile += task.item.itemInfo.bundlePath + "," + task.item.itemInfo.version + ",\r\n";
    //            SaveVersion(strVersionFile);


    //        }
    //        else
    //        {
    //            if (ExitUnCompress)
    //            {
    //                if (lstTask.Count == 0)
    //                {
    //                    ExitUnCompress = false;
    //                    break;
    //                }
    //            }
    //            else
    //            {
    //                Thread.Sleep(100);
    //            }
    //        }
    //    }
    //    Debug.Log("EndUnCompress");
    //}
    int SaveFileAsync(byte[] data, BundleGlobalItem item)
    {
        UnCompressTask task = new UnCompressTask();
        task.data = data;
        task.item = item;
        int taskCount = 0;
        lock (UnCompressLock)
        {
            lstTask.Add(task);
            taskCount = lstTask.Count;
        }
        return taskCount;
    }

//    IEnumerator UpdateAllBundle()
//    {
//        //if(!updating)
//        {



//            string xml_version = "";
//            if (version.Length == 0)
//            {
//                string svnVersion = AppVersion()["versionName"].Split('.')[3];

//                if (Application.platform == RuntimePlatform.Android)
//                {
//                    version = "0/";
//                }
//                else if (Application.platform == RuntimePlatform.IPhonePlayer)
//                {
//                    version = "10/";
//                }
//                else if (Application.platform == RuntimePlatform.WP8Player)
//                {
//                    version = "20/";
//                }
//                else if (Application.platform == RuntimePlatform.WindowsEditor)
//                {
//#if UNITY_ANDROID
//                    version = "0/";
//#endif

//#if UNITY_IPHONE
//                    version = "10/";
//#endif
//                }

//                version += svnVersion + "/";
//                xml_version = svnVersion;
//            }

//            cdnIndex = 0;


//            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
//            string url = GetServer() + "bundleinfo.xml" + xml_version;
//            byte[] xmlData = null;
//            if (InternalNetDebug)
//            {
//                xmlData = File.ReadAllBytes(url);
//            }
//            else
//            {

//                string bundleInfo = "";
//                while (true)
//                {
//                    long[] velocity = new long[cdn.Length];
//                    for (int i = 0; i < cdn.Length; i++)
//                    {
//                        url = GetServer() + "bundleinfo.xml" + xml_version;
//                        Debug.Log(url);
//                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
//                        watch.Start();
//                        wwwPercent = new WWW(url);
//                        yield return wwwPercent;
//                        if (wwwPercent.error != null)
//                        {
//                            velocity[i] = -1;
//                            Debug.Log(wwwPercent.error);
//                        }
//                        else
//                        {
//                            bundleInfo = wwwPercent.text;
//                            watch.Stop();
//                            velocity[i] = watch.ElapsedTicks;
//                        }
//                        ChangeCDN();
//                    }
//                    bool bValid = false;
//                    long last = long.MaxValue;
//                    for (int i = 0; i < cdn.Length; i++)
//                    {
//                        if (velocity[i] != -1 && velocity[i] < last)
//                        {
//                            last = velocity[i];
//                            cdnIndex = i;
//                            bValid = true;
//                        }
//                    }
//                    if (!bValid)
//                    {
//                        sdUICharacter.Instance.ShowLoginMsg("Can't Get Resource Version Information,Retry in 5 Second Later");
//                        yield return new WaitForSeconds(5.0f);
//                    }
//                    else
//                    {
//                        sdUICharacter.Instance.HideLoginMsg();
//                        break;
//                    }
//                }

//                Debug.Log("parse server bundleinfo.xml ");

//                xmlData = wwwPercent.bytes;
//                wwwPercent = null;

//            }

//            byte[] buffer = new byte[1024 * 1024];

//            MemoryStream dst = new MemoryStream(buffer, true);
//            MemoryStream src = new MemoryStream(xmlData);
//            SD.Decompress(src, dst);

//            string csvContent = Encoding.ASCII.GetString(buffer, 0, (int)dst.Position);
//            //Debug.Log(csvContent);

//            string[] lines = csvContent.Split('\n');
//            ParseBundleCSV(lines, ref bundles, bundleTable);


//            Debug.Log("load local bundleinfo.xml");
//            //加载本地xml..
//            //System.Xml.XmlDocument docLocal =new System.Xml.XmlDocument();
//            Hashtable downloadBundle = new Hashtable();
//            try
//            {
//                if (File.Exists(LocalPath + "bundleinfo.txt"))
//                {
//                    //docLocal.Load(local+"bundleinfo.xml");
//                    StreamReader r = File.OpenText(LocalPath + "bundleinfo.txt");
//                    while (!r.EndOfStream)
//                    {
//                        string line = r.ReadLine();
//                        if (line.Length > 0)
//                        {
//                            string[] strElement = line.Split(',');
//                            downloadBundle[strElement[0]] = uint.Parse(strElement[1]);
//                        }
//                        //localContent	 = docLocal.SelectSingleNode("Bundles");
//                    }
//                    r.Close();
//                }
//            }
//            catch (System.Exception e)
//            {
//                Debug.Log(e.Message + "load xml failed!");
//                //root1=null;
//            }

//            BundleGlobalItem[] updateBundles = null;
//            strVersionFile = "";
//            if (downloadBundle.Count > 0)
//            {

//                Debug.Log("delete unused bundle");
//                //删除上个版本中(不在这个版本中)的文件..
//                //BundleItem[] oldBundle	=	null;
//                //Hashtable oldTable		=	new Hashtable();
//                //ParseXmlBundle(root1,ref oldBundle,oldTable);



//                Debug.Log("Last Bundle Count = " + downloadBundle.Count);
//                //Debug.Log("Current Bundle Count = " + BundleIndex.Count);
//                foreach (DictionaryEntry de in downloadBundle)
//                {
//                    string name = de.Key as string;
//                    uint ver = (uint)de.Value;
//                    //Debug.Log("Compare Bundle Index ["+item.bundlePath+"]");
//                    if (!bundleTable.ContainsKey(name))
//                    {
//                        string strLocalPath = FixedPath(name);
//                        Debug.Log("delete bundle " + name + ver);
//                        File.Delete(strLocalPath);
//                        if (File.Exists(strLocalPath))
//                        {
//                            Debug.Log("delete bundle Failed! " + strLocalPath);
//                        }
//                    }
//                    else
//                    {
//                        //Debug.Log("bundle is exist! "+item.bundlePath + item.version);
//                    }
//                }


//                Debug.Log("find bundle need for update");
//                List<BundleGlobalItem> lstDownload = new List<BundleGlobalItem>();
//                //判断是否需要更新文件..
//                for (int i = 0; i < bundles.Length; i++)
//                {
//                    BundleGlobalItem item = bundles[i];
//                    if (item == null)
//                    {
//                        continue;
//                    }
//                    if (!downloadBundle.ContainsKey(item.itemInfo.bundlePath))
//                    {
//                        lstDownload.Add(item);
//                    }
//                    else
//                    {
//                        uint ver = (uint)downloadBundle[item.itemInfo.bundlePath];
//                        if (ver != item.itemInfo.version)
//                        {
//                            lstDownload.Add(item);
//                            continue;
//                        }

//                        if (!File.Exists(FixedPath(item.itemInfo.bundlePath)))
//                        {
//                            lstDownload.Add(item);
//                            continue;
//                        }
//                        strVersionFile += item.itemInfo.bundlePath + "," + item.itemInfo.version + ",\r\n";
//                    }

//                }
//                SaveVersion(strVersionFile);

//                Debug.Log("update some bundle = " + lstDownload.Count);

//                updateBundles = lstDownload.ToArray();


//            }
//            else
//            {

//                Debug.Log("update all bundle" + bundles.Length);
//                updateBundles = bundles;
//            }
//            if (updateBundles != null)
//            {

//                Thread UnCompressThread = new Thread(new ThreadStart(UnCompressMain));
//                UnCompressThread.Start();


//                for (int i = 0; i < updateBundles.Length;)
//                {
//                    OnDownload(i, updateBundles.Length);
//                    BundleGlobalItem item = updateBundles[i];
//                    Debug.Log("download bundle " + item.itemInfo.bundlePath);
//                    //guiText.text	=	i+"/"+BundleInfoArray.Length;
//                    //Download(BundleInfoArray[i]);

//                    string bundleurl = GetServer() + item.itemInfo.bundlePath.Replace("$", "__") + item.itemInfo.version.ToString();
//                    byte[] bundleData = null;
//                    if (InternalNetDebug)
//                    {
//                        yield return 0;

//                        if (!File.Exists(bundleurl))
//                        {
//                            Debug.Log("Bundle Don't Exist! " + bundleurl);
//                            i++;
//                            continue;
//                        }

//                        bundleData = File.ReadAllBytes(bundleurl);

//                    }
//                    else
//                    {
//                        wwwPercent = new WWW(bundleurl);
//                        yield return wwwPercent;
//                        if (wwwPercent.error != null)
//                        {
//                            Debug.Log(wwwPercent.error);
//                            if (wwwPercent.error.Contains("FileNotFoundException") ||
//                               wwwPercent.error.Contains("404: not found") ||
//                               wwwPercent.error.Contains("404 Not Found"))
//                            {
//                                sdUICharacter.Instance.HideLoginMsg();
//                                i++;
//                            }
//                            else
//                            {
//                                sdUICharacter.Instance.ShowLoginMsg(wwwPercent.error);
//                                ChangeCDN();
//                            }
//                            wwwPercent = null;
//                            continue;
//                        }
//                        else
//                        {
//                            //校验压缩之后的CRC是否匹配 不匹配则更换CDN 重新下载..
//                            BundleItem iteminfo = item.itemInfo;
//                            byte[] data = wwwPercent.bytes;
//                            uint comp_crc = SevenZip.CRC.CalculateDigest(data, 0, (uint)data.Length);
//                            if (comp_crc != iteminfo.compress_crc)
//                            {
//                                Debug.Log(iteminfo.bundlePath + " compress_crc doesn't match " + comp_crc + " " + iteminfo.compress_crc);
//                                sdUICharacter.Instance.ShowLoginMsg("crc doesn't match,download again!");
//                                ChangeCDN();
//                                continue;
//                            }
//                            else
//                            {
//                                sdUICharacter.Instance.HideLoginMsg();
//                            }
//                        }
//                        bundleData = wwwPercent.bytes;
//                    }

//                    SaveFileAsync(bundleData, item);

//                    wwwPercent = null;

//                    i++;
//                }

//            }

//            ExitUnCompress = true;
//            while (true)
//            {
//                if (ExitUnCompress)
//                {
//                    yield return 0;
//                }
//                else
//                {
//                    break;
//                }
//            }

//            Debug.Log("update finished!");

//            OnDownloadFinished();
//        }
//        StartCoroutine(updateLoad());
//    }
    void SaveVersion(string versionInfo)
    {
        string path = LocalPath;
        FileStream write = new FileStream(path + "bundleinfo.txt", FileMode.Create);
        byte[] data = Encoding.ASCII.GetBytes(versionInfo);
        write.Write(data, 0, data.Length);
        write.Close();
    }
    //void SaveFile(byte[] data, BundleItem item)
    //{


    //    string localPath = FixedPath(item.bundlePath);
    //    FileStream file = new FileStream(localPath, FileMode.Create);
    //    //file.Write(data,0,data.Length);
    //    if (item.isCompress)
    //    {
    //        SD.Decompress(new MemoryStream(data), file);
    //    }
    //    else
    //    {
    //        file.Write(data, 0, data.Length);
    //    }
    //    file.Close();
    //}
    AssetBundle LoadBundle(string name)
    {
        if (!bundleTable.ContainsKey(name))
        {
            return null;
        }
        int index = (int)bundleTable[name];
        return LoadBundle(index);
    }
    AssetBundle LoadBundle(int index)
    {
        if (bundles[index].bundle == null)
        {
            bundles[index].bundle = AssetBundle.LoadFromFile(FixedPath(bundles[index].itemInfo.bundlePath));
        }
        return bundles[index].bundle;
    }
}
