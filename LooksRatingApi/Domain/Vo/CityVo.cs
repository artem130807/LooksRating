using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Domain.Vo
{
    public class CityVo : ValueObject
    {
        public string Value {get;}

        [JsonConstructor]
        private CityVo(string city)
        {
            Value = city;
        }
        private CityVo(){}
        public static Result<CityVo> Create(string city)
        {
            if(string.IsNullOrWhiteSpace(city))
                return Result.Failure<CityVo>("Город не может быть пустым");
            return new CityVo(city);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}