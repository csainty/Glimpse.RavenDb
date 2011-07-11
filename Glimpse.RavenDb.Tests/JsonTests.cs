using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Glimpse.RavenDb.Tests
{
	public class JsonTests
	{
		[Fact]
		public void CanHandleNonJson() {
			var result = Profiler.ParseJsonResult("The server returned an Error 404.");
			Assert.Equal("The server returned an Error 404.", result);
		}

		[Fact]
		public void CanProcessString() {
			var result = Profiler.ParseJsonResult(@"{""key"":""data""}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);
			Assert.Equal("data", data[1][1]);
		}

		[Fact]
		public void CanProcessInt() {
			var result = Profiler.ParseJsonResult(@"{""key"":12}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);
			Assert.Equal((Int64)12, data[1][1]);
		}

		[Fact]
		public void CanProcessFloat() {
			var result = Profiler.ParseJsonResult(@"{""key"":12.34}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);
			Assert.Equal(12.34, data[1][1]);
		}

		[Fact]
		public void CanProcessBoolean() {
			var result = Profiler.ParseJsonResult(@"{""key"":true}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);
			Assert.Equal(true, data[1][1]);
		}

		[Fact]
		public void CanProcessSubObject() {
			var result = Profiler.ParseJsonResult(@"{""key"":{""subkey"":""data""}}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);

			Assert.IsType<List<object[]>>(data[1][1]);
			var subdata = data[1][1] as List<object[]>;
			Assert.Equal(2, subdata.Count);
			Assert.Equal("subkey", subdata[1][0]);
			Assert.Equal("data", subdata[1][1]);
		}

		[Fact]
		public void CanProcessValueArray() {
			var result = Profiler.ParseJsonResult(@"{""array"":[true,false,true]}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("array", data[1][0]);

			Assert.IsType<List<object[]>>(data[1][1]);
			var subdata = data[1][1] as List<object[]>;
			Assert.Equal(4, subdata.Count);
			Assert.Equal("Values", subdata[0][0]);
			Assert.Equal(true, subdata[1][0]);
			Assert.Equal(false, subdata[2][0]);
			Assert.Equal(true, subdata[3][0]);
		}

		[Fact]
		public void CanProcessObjectArray() {
			var result = Profiler.ParseJsonResult(@"{""array"":[{""key1"":""val1"",""key2"":""val2""},{""key1"":""val3"",""key2"":""val4""},{""key1"":""val5"",""key2"":""val6""}]}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("array", data[1][0]);

			Assert.IsType<List<object[]>>(data[1][1]);
			var subdata = data[1][1] as List<object[]>;
			Assert.Equal(4, subdata.Count);
			Assert.Equal("key1", subdata[0][0]);
			Assert.Equal("key2", subdata[0][1]);
			Assert.Equal("val1", subdata[1][0]);
			Assert.Equal("val2", subdata[1][1]);
			Assert.Equal("val3", subdata[2][0]);
			Assert.Equal("val4", subdata[2][1]);
			Assert.Equal("val5", subdata[3][0]);
			Assert.Equal("val6", subdata[3][1]);
		}

		[Fact]
		public void WillFilterFields() {
			Profiler.HideFields("filterThisField");
			var result = Profiler.ParseJsonResult(@"{""key"":""data"",""filterThisField"":""data""}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);
			Assert.Equal("data", data[1][1]);
		}

		[Fact]
		public void CanLoadCompleteDocument() {
			string json;
			using (var reader = new StreamReader(typeof(JsonTests).Assembly.GetManifestResourceStream("Glimpse.RavenDb.Tests.ExampleJSON.txt"))) {
				json = reader.ReadToEnd();
			}
			var result = Profiler.ParseJsonResult(json);
			Assert.NotNull(result);
		}

		[Fact]
		public void CanProcessInlineJson() {
			var result = Profiler.ParseJsonResult(@"{""key"":""{\""key\"":\""data\""}""}");
			Assert.NotNull(result);
			Assert.IsType<List<object[]>>(result);
			var data = result as List<object[]>;
			Assert.Equal(2, data.Count);
			Assert.Equal("key", data[1][0]);

			var data2 = data[1][1] as List<object[]>;
			Assert.NotNull(data2);
			Assert.Equal(2, data2.Count);
			Assert.Equal("key", data2[1][0]);
			Assert.Equal("data", data2[1][1]);
		}

	}
}