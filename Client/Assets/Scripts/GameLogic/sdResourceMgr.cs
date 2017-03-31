using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TaskParam
{
    public ResLoadDelegate _cb;
    public ResLoadParams _param;
}
public class ResourceTask
{
    public string _name;
    public Object _object;
    public ResLoadParams _param;
    public List<TaskParam> lstTask = new List<TaskParam>();
    public bool failed = false;
    public uint Ref = 0;
    public void AddCB(ResLoadDelegate cb, ResLoadParams param)
    {
        if (cb == null)
            return;
        TaskParam tp = new TaskParam();
        tp._cb = cb;
        tp._param = param;
        lstTask.Add(tp);
    }
    public void OnLoadFinished(Object obj)
    {
        _object = obj;
        if (_object == null)
        {
            failed = true;
        }
        foreach (TaskParam tp in lstTask)
        {
            if (tp._cb != null)
            {
                tp._cb(tp._param, _object);
            }
        }
        lstTask.Clear();
    }
}

public class sdResourceMgr : Singleton<sdResourceMgr>
{
    public Hashtable resourceDB = new Hashtable();
    uint index = 0;


    void AddTask(string orginPath, System.Type t, int priority)
    {
        //Debug.Log(orginPath);
        string path = orginPath;

        ResLoadParams __param = new ResLoadParams();
        __param.info = orginPath;


        LoadRequest loadobj = new LoadRequest();

        loadobj.param = __param;
        loadobj.callbackFunc = resourceLoadCallback;
        loadobj.path = path;
        loadobj.resType = t;
        loadobj.priority = priority;
        int tmpId = path.LastIndexOf("/");
        string resName = path;
        if (tmpId >= 0)
        {
            resName = path.Substring(tmpId + 1);
        }
        int dotIndex = resName.LastIndexOf(".");
        if (dotIndex >= 0)
        {
            resName = resName.Substring(0, dotIndex);
        }
        loadobj.resName = resName;

        int flagId = path.LastIndexOf("$");
        if (flagId >= 0)
        {
            int folderFlagId = path.IndexOf("/", flagId);
            string bundleName = path;
            if (folderFlagId >= 0)
            {
                bundleName = path.Substring(0, folderFlagId);
            }
            bundleName += ".unity3d";
            loadobj.bundleName = bundleName;

            BundleGlobal.Instance.LoadObject(loadobj);



        }
        else
        {
            Debug.Log("bundle isn't exist!" + path);
            return;
        }



    }
    public void PreLoadResource(string path)
    {
        __PreLoadResource(path, 0, typeof(Object));
    }
    public void PreLoadResource(string path, System.Type t)
    {
        __PreLoadResource(path, 0, t);
    }
    public void PreLoadResourceDontUnload(string path)
    {
        __PreLoadResource(path, 1, typeof(Object));
    }
    public void PreLoadResourceDontUnload(string path, System.Type t)
    {
        __PreLoadResource(path, 1, t);
    }
    void __PreLoadResource(string path, uint refCount, System.Type t)
    {
        if (!BundleGlobal.IsMobile())
        {
            return;
        }
        if (path.Length == 0)
        {
            return;
        }
        bool resourceExist = resourceDB.ContainsKey(path);
        if (resourceExist)
        {
            ResourceTask task = resourceDB[path] as ResourceTask;
            task.Ref = refCount;
        }
        else
        {


            ResourceTask task = new ResourceTask();
            task._name = path;
            task._param = null;
            task.Ref = refCount;
            resourceDB[path] = task;

            AddTask(path, t, -1);
        }
    }
    public void LoadResource(string path, ResLoadDelegate cb, ResLoadParams param)
    {
        LoadResource(path, cb, param, typeof(Object));
    }
    public void LoadResourceImmediately(string path, ResLoadDelegate cb, ResLoadParams param)
    {
        LoadResource(path, cb, param, typeof(Object), 1);
    }
    public void LoadResourceImmediately(string path, ResLoadDelegate cb, ResLoadParams param, System.Type t)
    {
        LoadResource(path, cb, param, t, 1);
    }
    public void LoadResource(string path, ResLoadDelegate cb, ResLoadParams param, System.Type t)
    {
        LoadResource(path, cb, param, t, 0);
    }
    public void LoadResource(string path, ResLoadDelegate cb, ResLoadParams param, System.Type t, int priority)
    {
        if (cb == null)
        {
            return;
        }
        if (param != null)
        {
            param._reqIndex = index++;
        }
        else
        {
            index++;
        }
        bool resourceExist = resourceDB.ContainsKey(path);
        if (resourceExist)
        {
            ResourceTask task = resourceDB[path] as ResourceTask;
            if (task._object != null || task.failed)
            {//资源已经加载了
                cb(param, task._object);
                //BundleGlobal.Instance.DoCallback(cb,param,task._object);
            }
            else
            {//资源正在加载
                task.AddCB(cb, param);
            }
        }
        else
        {//资源还没有加载，开始加载资源
            //Debug.Log(path);
            ResourceTask task = new ResourceTask();
            task._name = path;
            task._param = param;
            task.AddCB(cb, param);
            resourceDB[path] = task;

            AddTask(path, t, priority);
        }
    }
    public void ClearTaskRef()
    {
        foreach (DictionaryEntry item in resourceDB)
        {
            ResourceTask task = item.Value as ResourceTask;
            task.Ref = 0;
        }
    }
    public void Clear()
    {
        Hashtable table = resourceDB.Clone() as Hashtable;
        resourceDB.Clear();
        foreach (DictionaryEntry item in table)
        {
            ResourceTask task = item.Value as ResourceTask;
            if (task.lstTask.Count != 0 || task.Ref != 0)
            {
                resourceDB.Add(item.Key, item.Value);
            }
        }
        table.Clear();
    }
    public static void resourceLoadCallback(ResLoadParams param, Object obj)
    {
        string fileName = param.info;

        ResourceTask task = sdResourceMgr.Instance.resourceDB[fileName] as ResourceTask;
        if (task != null)
        {
            task.OnLoadFinished(obj);
        }
        //sdResourceMgr.Instance.resourceDB.Remove(fileName);
    }




}
