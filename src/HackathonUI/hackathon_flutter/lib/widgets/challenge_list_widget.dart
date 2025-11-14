// lib/widgets/challenge_list_widget.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/challenge_provider.dart';

// TODO: Przenieś 'Challenge' do osobnego pliku, np. 'lib/models/challenge.dart'
class Challenge {
  final String id;
  final String title;
  final String description;

  Challenge({required this.id, required this.title, required this.description});

  factory Challenge.fromJson(Map<String, dynamic> json) {
    return Challenge(
      id: json['id'],
      title: json['name'],
      description: json['description'],
    );
  }
}

class ChallengeListWidget extends StatelessWidget {
  // Nie potrzebujemy już StatefulWidget
  const ChallengeListWidget({super.key});

  @override
  Widget build(BuildContext context) {
    // Używamy 'context.watch' aby widget PRZEBUDOWAŁ SIĘ,
    // gdy 'notifyListeners()' zostanie wywołane (np. po zmianie strony).
    final provider = context.watch<ChallengeProvider>();

    // Pobieramy listę tylko dla bieżącej strony
    final challenges = provider.challengesForCurrentPage;

    // Nie potrzebujemy już 'initState' ani 'ScrollController'

    return Column(
      children: [
        // --- 1. LISTA WYZWAŃ (TERAZ MA TYLKO 10 ELEMENTÓW) ---
        ListView.builder(
          key: const PageStorageKey<String>('challengeList'), // Zapamiętuje pozycję przewijania
          shrinkWrap: true, // Kluczowe, gdy ListView jest w Column
          physics: const NeverScrollableScrollPhysics(), // Używamy przewijania z nadrzędnego
          itemCount: challenges.length,
          itemBuilder: (context, index) {
            final challenge = challenges[index];
            return Card(
              margin: const EdgeInsets.symmetric(vertical: 8.0),
              child: ListTile(
                title: Text(challenge.title,
                    style: const TextStyle(fontWeight: FontWeight.bold)),
                subtitle: Text(challenge.description,
                    maxLines: 2, overflow: TextOverflow.ellipsis),
                trailing: const Icon(Icons.arrow_forward_ios),
                onTap: () {
                  print('Naciśnięto wyzwanie: ${challenge.id}');
                  // TODO: Nawigacja do strony szczegółów
                },
              ),
            );
          },
        ),

        // --- 2. NOWE KONTROLKI PAGINACJI ---
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 16.0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              // Przycisk "Wstecz"
              IconButton(
                icon: const Icon(Icons.arrow_back_ios),
                // 'onPressed: null' automatycznie wyłącza (szary) przycisk
                onPressed: (provider.currentPage > 1)
                    ? () => context.read<ChallengeProvider>().previousPage()
                    : null,
              ),

              // Tekst "Strona 1 z 10"
              Text(
                'Strona ${provider.currentPage} z ${provider.totalPages}',
                style: const TextStyle(fontSize: 16),
              ),

              // Przycisk "Dalej"
              IconButton(
                icon: const Icon(Icons.arrow_forward_ios),
                onPressed: (provider.currentPage < provider.totalPages)
                    ? () => context.read<ChallengeProvider>().nextPage()
                    : null,
              ),
            ],
          ),
        ),
      ],
    );
  }
}