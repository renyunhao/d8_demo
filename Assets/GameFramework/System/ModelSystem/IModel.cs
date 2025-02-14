
public interface IModel
{
    void InitOnce();
    void LoadDataFromLocal();
    void LoadDataFromServer();
    void AfterLoadDataFromServer();
}
