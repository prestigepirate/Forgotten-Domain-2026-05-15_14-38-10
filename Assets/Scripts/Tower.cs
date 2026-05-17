using UnityEngine;

namespace ForgottenDomain
{
    public class Tower : MonoBehaviour
    {
        public int lifePoints = 10000;
        public Team OwnerTeam { get; set; }
        public int GridX { get; set; }
        public int GridZ { get; set; }
public bool IsDestroyed => lifePoints <= 0;

        public void Initialize(Team team, Vector3 position, int x, int z)
        {
            OwnerTeam = team;
            transform.position = position;
            GridX = x;
            GridZ = z;
            
            // Build visual if none exists
            if (GetComponentInChildren<MeshRenderer>() == null) BuildVisual();
        }

        private void BuildVisual()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Visual";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0, 1.5f, 0);
            body.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", OwnerTeam == Team.Player ? Color.blue : Color.red);
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(body.GetComponent<Collider>());
        }

        public void UpdateGridCoords()
        {
            if (transform.parent != null && transform.parent.name.StartsWith("Tile_"))
            {
                var parts = transform.parent.name.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
                {
                    GridX = x;
                    GridZ = z;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDestroyed) return;

            lifePoints -= amount;
            DamageText.Create(transform.position + Vector3.up * 2f, $"-{amount}", Color.red);
            
            if (lifePoints <= 0)
            {
                lifePoints = 0;
                OnTowerDestroyed();
            }
        }

        private void OnTowerDestroyed()
        {
            string winner = OwnerTeam == Team.Player ? "Opponent" : "Player";
            GameLogManager.Instance?.Log($"!!! {OwnerTeam}'s Tower has been DESTROYED !!!");
            GameLogManager.Instance?.Log($"--- {winner} WINS THE GAME ---");
            Debug.Log(gameObject.name + " has been destroyed! Game Over.");
            // Additional game over logic in GameManager or here
        }
}
}
