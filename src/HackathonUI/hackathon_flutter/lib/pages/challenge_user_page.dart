import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import 'package:provider/provider.dart';
import '../services/token_storage.dart';
import '../providers/challenge_provider.dart';

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
  Map<String, dynamic>? _challengeData;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadChallengeDetails();
  }

  Future<void> _loadChallengeDetails() async {
    final data = await context.read<ChallengeProvider>().fetchChallengeById(widget.challengeId);
    if (mounted) {
      setState(() {
        _challengeData = data;
        _isLoading = false;
      });
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        // 3. Używamy 'widget.challengeId', aby pokazać ID w tytule
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            context.go('/dashboard');
          },
        ),
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
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    
    if (_challengeData == null) {
      return const Center(
        child: Text('Nie udało się załadować szczegółów wyzwania'),
      );
    }
    String title = _challengeData?['title'] ?? 'Brak tytułu';
    String description = _challengeData?['description'] ?? 'Brak opisu';
    String data = _challengeData?['submissionDeadline'] ?? 'Brak danych';
    String isActive = _challengeData?['isActive'] == true ? 'Aktywne' : 'Nieaktywne';
    List<dynamic> allowedFileTypes = _challengeData?['allowedFileTypes'] ?? ['Dwolne'];
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 800),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              const SizedBox(height: 16),
              Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Text(
                    title,
                    style: Theme.of(context).textTheme.headlineLarge,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    isActive,
                    style: const TextStyle(
                      fontSize: 19,
                      color: Colors.grey,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
              const SizedBox(height: 24),
              Text(
                description,
                style: Theme.of(context).textTheme.headlineMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),
              Text(
                'Deadline: $data',
                style: Theme.of(context).textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),
              SizedBox(
                width: 300,
                child: ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14.0),
                    backgroundColor: AppColors.primary,
                  ),
                  onPressed: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Przesyłanie rozwiązania...'),
                      ),
                    );
                  },
                  child: const Text(
                    'Prześlij rozwiązanie',
                    style: TextStyle(
                      fontSize: 16,
                      color: AppColors.background,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                'Dozwolone typy plików: ${allowedFileTypes.toString()}',
                style: Theme.of(context).textTheme.labelMedium,
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}