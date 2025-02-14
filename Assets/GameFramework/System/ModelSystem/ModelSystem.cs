using System;
using System.Collections.Generic;

/// <summary>
/// 数据模块管理类
/// </summary>
public class ModelSystem
{
    private static List<IModel> modelList = new List<IModel>();

    public static void Init()
    {
        var types = typeof(ModelSystem).Assembly.GetTypes();
        foreach (var type in types)
        {
            if (type.IsClass)
            {
                var interfaces = type.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (iface == typeof(IModel))
                    {
                        modelList.Add(Activator.CreateInstance(type) as IModel);
                    }
                }
            }
        }
    }

    public static void InitOnce()
    {
        foreach (var model in modelList)
        {
            model.InitOnce();
        }
    }

    public static void LoadDataFromLocal()
    {
        foreach (var model in modelList)
        {
            model.LoadDataFromLocal();
        }
    }

    public static void LoadDataFromServer()
    {
        foreach (var model in modelList)
        {
            model.LoadDataFromServer();
        }
    }

    public static void AfterLoadDataFromServer()
    {
        foreach (var model in modelList)
        {
            model.AfterLoadDataFromServer();
        }
    }
}
