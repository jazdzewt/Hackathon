import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import '../widgets/challenge_list_widget.dart';
import 'package:smooth_page_indicator/smooth_page_indicator.dart';
import 'package:provider/provider.dart';
import '../providers/challenge_provider.dart';
import 'package:go_router/go_router.dart';
import '../services/token_storage.dart';


class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {

  final List<String> imgList = const [
    'assets/images/hackaton1.jpg',
    'assets/images/hackaton2.jpg',
    'assets/images/hackaton3.jpg',
  ];

  // sledzenie aktywnego indeksu dla wskaznika
  int activeIndex = 0;

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      context.read<ChallengeProvider>().loadStateFromCache();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Welcome to Goldman Sachs Hackathon!'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: TextButton.icon(
              onPressed: () async {
                await TokenStorage.deleteToken();
                if (context.mounted) {
                  context.go('/');
                }
              },
              icon: const Icon(Icons.logout, color: Colors.white),
              label: const Text(
                'Log out',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (imgList.isNotEmpty)
            
              Column(
                children: [
                  CarouselSlider(
                    options: CarouselOptions(
                      autoPlay: true,
                      aspectRatio: 16 / 9,
                      viewportFraction: 0.75,
                      enlargeCenterPage: true,
                      onPageChanged: (index, reason) {
                        setState(() {
                          activeIndex = index;
                        });
                      },
                    ),
                    items: imgList.map((item) {
                      return Container(
                        margin: const EdgeInsets.symmetric(horizontal: 5.0),
                        width: double.infinity,
                        child: ClipRRect(
                          borderRadius: const BorderRadius.all(Radius.circular(10.0)),
                          child: Image.asset(
                            item,
                            fit: BoxFit.cover,
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                  
                  const SizedBox(height: 12),
                  AnimatedSmoothIndicator(
                    activeIndex: activeIndex,
                    count: imgList.length,
                    effect: WormEffect(
                      dotHeight: 10,
                      dotWidth: 10,
                      activeDotColor: Theme.of(context).colorScheme.primary,
                      dotColor: Colors.grey.shade400,
                    ),
                  ),
                ],
              ),

            const Padding(
              padding: EdgeInsets.fromLTRB(16.0, 24.0, 16.0, 8.0),
              child: Text(
                'Available Challenges',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
            ),
            
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
              child: TextField(
                decoration: InputDecoration(
                  hintText: 'Search challenges...',
                  prefixIcon: const Icon(Icons.search),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8.0),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8.0),
                    borderSide: BorderSide(color: AppColors.primary, width: 2.0),
                  ),
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 12.0),
                ),
                onChanged: (value) {
                  context.read<ChallengeProvider>().setSearchQuery(value);
                },
              ),
            ),
            
            FutureBuilder<Map<String, dynamic>?>(
              future: context.read<ChallengeProvider>().fetchCurrentUser(),
              builder: (context, snapshot) {
                if (snapshot.data?['isAdmin'] == true) {
                  return Padding(
                    padding: const EdgeInsets.fromLTRB(16.0, 8.0, 16.0, 8.0),
                    child: ElevatedButton.icon(
                      onPressed: () {
                        context.go('/challengeCreate');
                      },
                      icon: const Icon(Icons.add),
                      label: const Text('Add New Challenge'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                      ),
                    ),
                  );
                }
                return const SizedBox.shrink();
              },
            ),
            
            const Divider(height: 1, indent: 16, endIndent: 16),

            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: ChallengeListWidget(),
            ),
          ],
        ),
      ),
    );
  }
}