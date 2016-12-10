using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace sql_serialisation_benchmarker
{
	public class DataReaderSerialisation
	{
		private static T SerialiseToObject<T>(SqlDataReader reader)
			where T : new()
		{
			if (!reader.Read())
			{
				throw new ArgumentException("No data in the data reader", "reader");
			}
			if (reader.FieldCount == 0)
			{
				throw new ArgumentException("No columns in the reader", "reader");
			}
			T newObj = new T();
			for (int i = 0; i < reader.FieldCount; i++)
			{
				setProperty(newObj, reader.GetName(i), reader.GetValue(i));
			}
			return newObj;
		}

		private static T SerialiseToObject<T>(IDataRecord dataRecord)
			where T : new()
		{
			if (dataRecord.FieldCount == 0)
			{
				throw new ArgumentException("No columns in the dataRecord", "dataRecord");
			}
			T newObj = new T();
			for (int i = 0; i < dataRecord.FieldCount; i++)
			{
				setProperty(newObj, dataRecord.GetName(i), dataRecord.GetValue(i));
			}
			return newObj;
		}

		private static IList<T> SerialiseToList<T>(SqlDataReader reader)
			where T : new()
		{
			List<T> list = new List<T>();
			if (!reader.Read())
			{
				throw new ArgumentException("No data in the data reader", "reader");
			}
			do
			{
				list.Add(SerialiseToObject<T>((IDataRecord)reader));
			} while (reader.Read());
			return list;
		}

		private static void setProperty<T>(T obj, string name, object value)
			where T : new()
		{
			var propOfObj = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			if (propOfObj != null)
			{
				if (propOfObj.PropertyType.IsEnum)
				{
					object[] parameters = new object[] { value.ToString(), null };
					object result = typeof(Enum).GetMethods().Where(m => m.Name == "TryParse")
						.Select(m => new { Method = m, Params = m.GetParameters(), Args = m.GetGenericArguments() })
						.Where(x => x.Params.Length == 2 && x.Args.Length == 1 && x.Params[1].ParameterType.GetElementType() == x.Args[0])
						.Select(x => x.Method)
						.First()
						.MakeGenericMethod(propOfObj.PropertyType)
						.Invoke(null, parameters);

					if ((bool)result)
					{
						propOfObj.SetValue(obj, Convert.ChangeType(parameters[1], propOfObj.PropertyType), null);
					}
				}
				else
				{
					propOfObj.SetValue(obj, Convert.ChangeType(value, propOfObj.PropertyType));
				}
			}
		}
	}
}
