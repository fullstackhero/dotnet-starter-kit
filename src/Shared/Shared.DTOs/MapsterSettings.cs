namespace DN.WebApi.Shared.DTOs
{
    public class MapsterSettings
    {
        public static void Configure()
        {
            // here we will define the type conversion / Custom-mapping
            // More details at https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping

            // for example
            /*TypeAdapterConfig<TSource, TDestination>
                .NewConfig()
                .Map(dest => dest.Gender,      //Genders.Male or Genders.Female
                    src => src.GenderString); //"Male" or "Female"*/

        }
    }
}