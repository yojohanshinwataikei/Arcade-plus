
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Util.Pooling{
	public interface IPoolable
	{
		// If this component ready to be reused
		bool Available{get;}
	}
	public class GameObjectPool<TComponent> where TComponent:MonoBehaviour,IPoolable{
		private GameObject prefab;
		private Transform root;
		private List<TComponent> pool=new List<TComponent>();
		public GameObjectPool(GameObject prefab,Transform root,int initialCapacity){
			this.prefab=prefab;
			this.root=root;
			for(int i=0;i<initialCapacity;i++){
				ExpandAndGetNew();
			}
		}
		private TComponent ExpandAndGetNew(){
			TComponent component = GameObject.Instantiate(prefab, root).GetComponent<TComponent>();
			pool.Add(component);
			return component;
		}
		// The Initializer is used to ensure Available is false after get, to avoid programming error
		public delegate void Initializer(TComponent component);
		public TComponent Get(Initializer initializer){
			foreach(TComponent component in pool){
				if(component.Available){
					initializer(component);
					return component;
				}
			}
			TComponent newComponent=ExpandAndGetNew();
			initializer(newComponent);
			return newComponent;
		}
		// This is used to modify all gameobject in the pool, like replace skin
		// Note that it is not needed to manually modify the prefab since this method do that for the invoker
		public delegate void Modifier(TComponent component);
		public void Modify(Modifier modifier){
			modifier(prefab.GetComponent<TComponent>());
			foreach(TComponent component in pool){
				modifier(component);
			}
		}
	}
}
