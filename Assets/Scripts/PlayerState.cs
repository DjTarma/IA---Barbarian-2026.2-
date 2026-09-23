public enum PlayerState
{
    Idle,           // Sem se mover
    Walk,           // Andar, usando WASD
    Run,            // Correr (Usar shift + WASD)
    RunBackwards,   // Não inserido no momento...
    CrouchIdle,     // Agachar, sem se mover
    CrouchRun,      // Andar enquanto está agachado
    Attack,         // Attack1
    Kick,           // Kick
    JumpAttack,     // Tecla de espaço
    BlockMid,       // Não inserido...
    BlockStart,     // Não inserido ainda
    CastSpell,      // Não inserido, pois não há uma lógica pra termos magia em um melee
    FrontFlip,      // Não inserido.
    Scream,         // Jogador "se recarrega" para soltar os Special1 e Special2. Use a Tecla Q.
    UnSheath,       // Iria ter o mesmo efeito do Scream.
    Pummel,         // Não inserido.
    QuickShot,      // Não inserido.
    QuickSlide,     // Estado inserido CASO o jogador esteja andando agachado.
    Rolling,        // Estado de rolagem quando o jogador anda. Ele rola no ar, tipo um pulo com roll.
    SlideStart,     // Não inserido.
    Special1,       // Usando a Tecla R para liberar especial 1. Use o Scream para liberar esse ataque.
    Special2,       // Usando a Tecla T para liberar especial 2. Use o Scream para liberar esse ataque.
    TakeDamage,     // Estado que o jogador recebe dano. Você tem cinco pontos de vida, se perder, entra o State Die
    Die             // Estado de morte :eyes: :skull:
}