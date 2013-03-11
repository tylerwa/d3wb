using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Armory
{

  public class Career
	{

		public event SendUpdateEventHandler SendUpdate;
		public delegate void SendUpdateEventHandler(string text);

		public int Count {
			get { return _heroes.Count; }
		}

		public List<IHero> Heroes {
			get { return _heroes; }
		}


		private readonly List<IHero> _heroes = new List<IHero>();
		public void Initialize(string battleTag)
		{
			JObject jo = default(JObject);
			string url = null;
			string host = "http://us.battle.net/api/d3/profile/" + battleTag + "/";
			jo = GetJObject(host);
			foreach (object obj_loopVariable in jo("heroes")) {
				obj = obj_loopVariable;
				url = host + "hero/" + obj("id").Value<int>();
				_heroes.Add(new Hero(url));
				if (SendUpdate != null) {
					SendUpdate("Loaded " + _heroes.Last.Name + " <" + _heroes.Last.ParagonLevel + ">");
				}
			}
		}


		private class Hero : IHero
		{

			public List<IActive> ActiveSkills {
				get { return _activeSkills; }
			}
			public List<IPassive> PassiveSkills {
				get { return _passiveSkills; }
			}
			public List<IProp<object>> Stats {
				get { return _props; }
			}
			public List<IItem> Items {
				get { return _items; }
			}
			public double Id {
				get { return GetVal<double>("id"); }
			}
			public string Name {
				get { return GetVal<string>("name"); }
			}
			public string Class {
				get { return GetVal<string>("class"); }
			}
			public double Gender {
				get { return GetVal<double>("gender"); }
			}
			public double Level {
				get { return GetVal<double>("level"); }
			}
			public double ParagonLevel {
				get { return GetVal<double>("paragonLevel"); }
			}
			public bool Hardcore {
				get { return GetVal<bool>("hardcore"); }
			}
			public bool Dead {
				get { return GetVal<bool>("dead"); }
			}
			public double Lastupdated {
				get { return GetVal<double>("last-updated"); }
			}
			double IHero.LastUpdated {
				get { return Lastupdated; }
			}

			private readonly JObject _profile;
			private readonly List<IActive> _activeSkills = new List<IActive>();
			private readonly List<IPassive> _passiveSkills = new List<IPassive>();
			private readonly List<IProp<object>> _props = new List<IProp<object>>();

			private readonly List<IItem> _items = new List<IItem>();

			public Hero()
			{
			}

			public Hero(string url)
			{
				JObject jo = null;
				jo = Career.GetJObject(url);
				_profile = jo;
				foreach (object obj_loopVariable in jo) {
					obj = obj_loopVariable;
					_props.Add(new Prop<object>(jo, obj.Key));
				}
				jo = _profile("stats");
				foreach (object obj_loopVariable in jo) {
					obj = obj_loopVariable;
					_props.Add(new Prop<object>(jo, obj.Key));
				}
				jo = _profile("items");
				if (jo.HasValues) {
					foreach (object obj_loopVariable in jo) {
						obj = obj_loopVariable;
						_items.Add(new Item(jo, obj.Key));
					}
				}
				//ImportSkills()
				//CalculateAllResist()
			}

			private T GetVal<T>(string name)
			{
				foreach (object p_loopVariable in _props) {
					p = p_loopVariable;
					if (p.Name == name) {
						return p.Value.value;
					}
				}
			}

			private class Prop<T> : IProp<T>
			{

				public T Value {
					get { return _value; }
					set { _value = value; }
				}

				public string Name {
					get { return _name; }
				}
				public string RangeName {
					get { return _rangeName; }
				}

				private readonly string _name;
				private readonly string _rangeName;

				private T _value;
				/// <summary>
				/// Initializes a new instance of the class.
				/// </summary>
				/// <param name="obj">The obj.</param>
				/// <param name="name">The name.</param>
				public Prop(JObject obj, string name)
				{
					_name = name;
					_rangeName = "Hero_" + name;
					_value = obj(name).Value<T>();
				}

			}

			private class Item : IItem
			{
				public Dictionary<string, IAttribute> Attributes {
					get { return _attr; }
				}
				public Dictionary<string, string> Data {
					get { return _data; }
				}
				public string Slot {
					get { return _slot; }
				}
				private readonly string _slot;
				private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
				private readonly Dictionary<string, IAttribute> _attr = new Dictionary<string, IAttribute>();
				public Item(JObject itemsJObj, string slot)
				{
					JObject dataJObj = itemsJObj(slot);
					JObject attrJObj = default(JObject);
					_slot = slot;
					_data.Add("name", dataJObj("name").Value<string>());
					_data.Add("icon", dataJObj("icon").Value<string>());
					_data.Add("displayColor", dataJObj("displayColor").Value<string>());
					_data.Add("tooltipParams", dataJObj("tooltipParams").Value<string>());
					attrJObj = Career.GetJObject("http://us.battle.net/api/d3/data/" + _data["tooltipParams"]);
					if (attrJObj != null) {
						attrJObj = attrJObj("attributesRaw");
						foreach (object obj_loopVariable in attrJObj) {
							obj = obj_loopVariable;
							_attr.Add(obj.Key, new Attribute(_slot, obj));
						}
					}
				}
				private class Attribute : IAttribute
				{
					public double Value {
						get { return _value; }
						set { _value = value; }
					}
					public string Name {
						get { return _name; }
						set { _name = value; }
					}
					public string RangeName {
						get { return _rangeName; }
						set { _rangeName = value; }
					}
					public bool Shown {
						get { return _shown; }
						set { _shown = value; }
					}
					public string Slot {
						get { return _slot; }
						set { _slot = value; }
					}
					private string _slot;
					private double _value;
					private string _name;
					private string _rangeName;
					private bool _shown = false;
					public Attribute(string slot, KeyValuePair<string, JToken> attr)
					{
						_slot = slot;
						_name = attr.Key;
						_value = attr.Value("max").Value<double>();
						_rangeName = _slot + "_" + Regex.Replace(_name, "#|!|@|&", "_");
					}
				}
			}

		}

		private static JObject GetJObject(string url)
		{
			JObject jo = null;
			Uri uri = null;
			string json = "";
			dynamic client = new WebClient();
			client.Proxy = null;
			uri = new Uri(url);
			do {
				try {
					json = client.DownloadString(uri);
					jo = JObject.Parse(json);
				} catch (Exception ex) {
					jo = null;
				}
			} while (!(jo != null));
			return jo;
		}

	}

}
