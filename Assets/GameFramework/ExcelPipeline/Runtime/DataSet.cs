using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace GameFramework
{
    public class DataSet<T>
    {
        public List<T> tempList;
        public List<T> dataList;
        public Dictionary<int, T> dataDic = new Dictionary<int, T>();

        public void SetData(List<T> list)
        {
            if (list != null && list.Count > 0)
            {
                if (dataList == null)
                {
                    dataList = new List<T>();
                }
                dataList.Clear();
                dataList.AddRange(list);
                if (dataDic == null)
                {
                    dataDic = new Dictionary<int, T>();
                }
                dataDic.Clear();
                foreach (T instance in list)
                {
                    TableDataBase dataBase = instance as TableDataBase;
                    if (dataDic.ContainsKey(dataBase.id))
                    {
                        Debug.LogError($"表{typeof(T).Name}中存在重复的id:{dataBase.id}");
                    }
                    else
                    {
                        dataDic.Add(dataBase.id, instance);
                    }
                }
            }
            else
            {
                Debug.LogError($"数据集{typeof(T).Name}在赋值时，源数据为空");
            }
        }

        public List<T> GetAllData()
        {
            if (tempList == null)
            {
                tempList = new List<T>();
            }
            tempList.Clear();
            tempList.AddRange(dataList);
            return tempList;
        }

        public T GetDataByID(int id)
        {
            if (dataDic != null && dataDic.Count > 0)
            {
                if (dataDic.ContainsKey(id))
                {
                    return dataDic[id];
                }
            }
            return default;
        }

        public bool TryGetDataByID(int id, out T data)
        {
            return dataDic.TryGetValue(id, out data);
        }
    }
}