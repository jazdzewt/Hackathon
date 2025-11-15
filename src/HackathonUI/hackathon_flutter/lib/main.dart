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
          path: '/challenge/:id', // id param
          builder: (context, state) {
            final challengeId = state.pathParameters['id']!;
            return ChallengeUserPage(challengeId: challengeId);
          },
        ),
        GoRoute(
          path: '/challengeAdmin/:id', // id param
          builder: (context, state) {
            final challengeId = state.pathParameters['id']!;
            return ChallengeAdminPage(challengeId: challengeId);
          },
        ),
      ],
      redirect: (context, state) async {
        final String location = state.matchedLocation;
        final bool loggedIn = await TokenStorage.isLoggedIn();

        final bool goingToAuthFree =
            location == '/' || location == '/register';

        // Not logged in and trying to access protected route → go to login
        if (!loggedIn && location.startsWith('/dashboard')) {
          return '/';
        }

        // Logged in and on auth pages → push to dashboard
        if (loggedIn && goingToAuthFree) {
          return '/dashboard';
        }

        return null; // no redirect
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
