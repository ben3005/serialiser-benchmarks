using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace sql_serialisation_benchmarker
{
	public class DataTableSerialization
	{

		/// <summary>
		/// Serialises each row in a data table to an instance 
		/// of the specified type
		/// </summary>
		/// <typeparam name="T">The type to create for each row</typeparam>
		/// <param name="dataTable">The data to serialise</param>
		/// <returns>A serialised list of items</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static IList<T> SerialiseToList<T>(DataTable dataTable)
			where T : new()
		{
			if (dataTable == null)
			{
				throw new ArgumentNullException("dataTable");
			}
			IList<T> list = new List<T>();

			if (dataTable.Rows.Count > 0)
			{
				foreach (DataRow row in dataTable.Rows)
				{
					list.Add(SerialiseToObject<T>(row));
				}
			}

			return list;
		}

		/// <summary>
		/// Serializes the first row of a data table
		/// to a specified object
		/// </summary>
		/// <typeparam name="T">The type to serialise the row to</typeparam>
		/// <param name="dataTable">The data to serialise</param>
		/// <returns>A serialised object with data from the first row</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static T SerialiseToObject<T>(DataTable dataTable, int RowToSerialise = 0)
			where T : new()
		{
			if (dataTable == null)
			{
				throw new ArgumentNullException("dataTable");
			}
			if (dataTable.Rows.Count < 1)
			{
				throw new ArgumentException("Must have at least one row in data table", "dataTable");
			}
			return SerialiseToObject<T>(dataTable.Rows[RowToSerialise]);
		}

		/// <summary>
		/// Serialises a row into a specified object
		/// by using the column names to set properties
		/// </summary>
		/// <typeparam name="T">The type to serialise the row to</typeparam>
		/// <param name="dataRow">The row of data</param>
		/// <returns>A serialised object form the row</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static T SerialiseToObject<T>(DataRow dataRow)
			where T : new()
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException("dataRow");
			}
			if (dataRow.Table == null)
			{
				throw new ArgumentException("Row is missing Table", "dataRow");
			}

			T obj = new T();

			foreach (DataColumn column in dataRow.Table.Columns)
			{
				PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName, BindingFlags.Public | BindingFlags.Instance);
				if (prop != null && prop.CanWrite)
				{
					if (prop.PropertyType.IsEnum)
					{
						object[] parameters = new object[] { dataRow[column.ColumnName].ToString(), null };
						object result = typeof(Enum).GetMethods().Where(m => m.Name == "TryParse")
							.Select(m => new { Method = m, Params = m.GetParameters(), Args = m.GetGenericArguments() })
							.Where(x => x.Params.Length == 2 && x.Args.Length == 1 && x.Params[1].ParameterType.GetElementType() == x.Args[0])
							.Select(x => x.Method)
							.First()
							.MakeGenericMethod(prop.PropertyType)
							.Invoke(null, parameters);

						if ((bool)result)
						{
							prop.SetValue(obj, Convert.ChangeType(parameters[1], prop.PropertyType), null);
						}
					}
					else
					{
						prop.SetValue(obj, Convert.ChangeType(dataRow[column.ColumnName], prop.PropertyType), null);
					}
				}
			}
			return obj;
		}
	}
}
