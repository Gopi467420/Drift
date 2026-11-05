

namespace Enums
{

    public enum CollectableSpawnPathType
    {
        None, Single, Random, Inline, Spiral, Cluster, SlantedRight, SlantedLeft, TwoPaths
    }
    public enum ZoneType
    {
        None, Open, Loose, Busy, Crowded, Choked
    }

    public enum CollectableType
    {
       C1, C2, C3, C4

    }


    public struct Rewards
    {

        public Rewards(float _stamina, float _score, float _scoreMult)
        {
            this.stamina = _stamina;
            this.score = _score;
            this.scoreMult = _scoreMult;
        }
       private float stamina;
       private float score;
       private float scoreMult;

        public float GetStamina() { return stamina; }
        public float GetScore() { return score; }
        public float GetScoreMult() { return scoreMult; }


        public void SetStamina(float value) { stamina = value; }
        public void SetScore(float value) { score = value; }
        public void SetScoreMult(float value) { scoreMult = value; }



    }
  


}