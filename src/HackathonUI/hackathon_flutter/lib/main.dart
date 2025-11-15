import 'package:flutter/material.dart';
import 'pages/dashboard_page.dart';
import 'package:provider/provider.dart'; 
import 'providers/challenge_provider.dart'; 
import 'pages/landing.dart';
import 'theme/colors.dart';
import 'package:go_router/go_router.dart';
import 'pages/register.dart';
import 'services/token_storage.dart';
import 'pages/challenge_user_page.dart';
import 'pages/challenge_admin_page.dart';
import 'pages/challenge_create_page.dart';

void main() {
  runApp(ChangeNotifierProvider(
      create: (context) => ChallengeProvider(),
      child: MyApp(),
    ),
  );
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    final GoRouter _router = GoRouter(
      initialLocation: '/',
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: '/register',
          builder: (context, state) => const RegisterScreen(),
        ),
        GoRoute(
          path: '/dashboard',
          builder: (context, state) => const DashboardPage(),
        ),
        GoRoute(
          path: '/challenge/:id',
          builder: (context, state) {
            final challengeId = state.pathParameters['id']!;
            return ChallengeUserPage(challengeId: challengeId);
          },
        ),
        GoRoute(
          path: '/challengeAdmin/:id',
          builder: (context, state) {
            final challengeId = state.pathParameters['id']!;
            return ChallengeAdminPage(challengeId: challengeId);
          },
        ),
        GoRoute(
          path: '/challengeCreate',
          builder: (context, state) => const ChallengeCreatePage(),
        ),
      ],
      redirect: (context, state) async {
        final String location = state.matchedLocation;
        final bool loggedIn = await TokenStorage.isLoggedIn();

        // Public routes - dostępne bez logowania
        final bool goingToAuthFree =
            location == '/' || location == '/register';

        // Protected routes - wymagają logowania
        final bool goingToProtected = 
            location.startsWith('/dashboard') || 
            location.startsWith('/challenge') ||
            location.startsWith('/challengeAdmin') ||
            location.startsWith('/challengeCreate');

        // Jeśli nie zalogowany i próbuje wejść na chronioną stronę -> redirect na landing
        if (!loggedIn && goingToProtected) {
          return '/';
        }

        // Jeśli zalogowany i próbuje wejść na landing/register -> redirect na dashboard
        if (loggedIn && goingToAuthFree) {
          return '/dashboard';
        }

        return null;
      },
    );
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Goldman Sachs Hackathon',
      theme: ThemeData(
        appBarTheme: const AppBarTheme(
          backgroundColor: AppColors.primary,
          foregroundColor: AppColors.background,
        ),
        textTheme: const TextTheme(
          headlineLarge: TextStyle(
            fontSize: 32,
            fontWeight: FontWeight.bold,
            color: AppColors.text,
          ),
        ),
      ),
      routerConfig: _router,
    );
  }
}
