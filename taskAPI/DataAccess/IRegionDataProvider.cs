namespace taskAPI.DataAccess;

public interface IRegionDataProvider
{
    RegionData Get(string locale);
}