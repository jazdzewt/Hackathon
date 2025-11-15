import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
// TODO: Popraw ścieżkę do TokenStorage, jeśli jest inna
import '../services/token_storage.dart'; 

class ChallengeUserPage extends StatefulWidget {
  // 1. Odbieramy 'challengeId' przekazane z routera
  final String challengeId;
  
  const ChallengeUserPage({
    super.key,
    required this.challengeId, // Jest to wymagane
  });

  @override
  State<ChallengeUserPage> createState() => _ChallengeUserPageState();
}

// 2. POPRAWIONA DEKLARACJA STANU
class _ChallengeUserPageState extends State<ChallengeUserPage> {
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        // 3. Używamy 'widget.challengeId', aby pokazać ID w tytule
        title: Text('Wyzwanie #${widget.challengeId}'),
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