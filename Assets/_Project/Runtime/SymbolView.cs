using UnityEngine;

namespace CoffinEscape
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class SymbolView : MonoBehaviour
    {
        [SerializeField] private SymbolId symbol;
        [SerializeField] private float size=0.08f;
        public void Set(SymbolId id,float diameter,Material material=null)
        {
            symbol=id; size=diameter;
            var mf=GetComponent<MeshFilter>(); mf.sharedMesh=BuildMesh(id,diameter);
            if(material!=null)GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        public static Mesh BuildMesh(SymbolId id,float diameter)
        {
            float r=diameter*.5f; int sides=id==SymbolId.Circle?32:id==SymbolId.Triangle?3:4;
            var vertices=new Vector3[sides+1]; var triangles=new int[sides*3]; vertices[0]=Vector3.zero;
            for(int i=0;i<sides;i++) { float a=Mathf.PI*2f*i/sides-Mathf.PI*.5f;
                vertices[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);
                triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=(i+1)%sides+1;
            }
            var mesh=new Mesh { name=$"{id} Symbol" }; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); return mesh;
        }
    }
}
