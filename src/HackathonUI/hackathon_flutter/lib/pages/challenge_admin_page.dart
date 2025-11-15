// --- 1. DODANE BRAKUJĄCE IMPORTY ---
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
// TODO: Popraw ścieżkę do TokenStorage, jeśli jest inna
import '../services/token_storage.dart'; 

class ChallengeAdminPage extends StatefulWidget {
  // --- 2. DODANY PARAMETR 'challengeId' ---
  final String challengeId;

  const ChallengeAdminPage({
    super.key,
    required this.challengeId, // Router teraz poprawnie przekaże ID
  });

  @override
  State<ChallengeAdminPage> createState() => _ChallengeAdminPageState();
}

// 3. POPRAWIONA DEKLARACJA STANU
class _ChallengeAdminPageState extends State<ChallengeAdminPage> {
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        // 4. Teraz 'widget.challengeId' będzie działać
        title: Text('Wyzwanie ALE ADMINOWE #${widget.challengeId}'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: TextButton.icon(
              onPressed: () async {
                await TokenStorage.deleteToken();
                if (context.mounted) {
                  // Wracamy do strony logowania/głównej
                  context.go('/'); 
                }
              },
              icon: const Icon(Icons.logout, color: Colors.white),
              label: const Text(
                'Wyloguj się',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ),
        ],
      ),
      // 4. "Pusty szkielet" body
      body: Center(
        child: Text('Tutaj będą szczegóły wyzwania: ${widget.challengeId}'),
      ),
    );
  }
}