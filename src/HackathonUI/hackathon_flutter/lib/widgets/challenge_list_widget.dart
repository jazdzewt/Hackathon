import 'package:flutter/material.dart';
import 'package:provider/provider.dart';  
import '../providers/challenge_provider.dart';

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
  const ChallengeListWidget({super.key});

  @override
  Widget build(BuildContext context) {
    // Używamy 'context.watch' aby widget PRZEBUDOWAŁ SIĘ,
    // gdy 'notifyListeners()' zostanie wywołane po zmianie strony
    final provider = context.watch<ChallengeProvider>();
    final challenges = provider.challengesForCurrentPage;


    return Column(
      children: [
        ListView.builder(
          key: const PageStorageKey<String>('challengeList'), 
          shrinkWrap: true, 
          physics: const NeverScrollableScrollPhysics(), 
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
                  context.go('/challenge/${challenge.id}');
                },
              ),
            );
          },
        ),

        
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 16.0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              IconButton(
                icon: const Icon(Icons.arrow_back_ios),
                onPressed: (provider.currentPage > 1)
                    ? () => context.read<ChallengeProvider>().previousPage()
                    : null,
              ),
              Text(
                'Strona ${provider.currentPage} z ${provider.totalPages}',
                style: const TextStyle(fontSize: 16),
              ),
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